using System.Reflection;
using NUnit.Framework;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ArchitectureTests
{
	private static readonly string[] _promptException = ["EtcdTerminal.App.Engine.Prompt"];

	[Test]
	public void AppTypesDoNotReferenceSpectre()
	{
		var appAssembly = typeof(App.Screens.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			if (_promptException.Contains(type.FullName))
				continue;

			foreach (var referenced in GetReferencedTypes(type))
				if (IsSpectreType(referenced))
					violations.Add($"{type.FullName} -> {referenced.FullName}");
		}

		Assert.That(violations, Is.Empty);
	}

	[Test]
	public void ScreensDoNotReferenceInfrastructure()
	{
		var appAssembly = typeof(App.Screens.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			if (type.Namespace?.StartsWith("EtcdTerminal.App.Screens", StringComparison.Ordinal) is not true)
				continue;

			foreach (var referenced in GetReferencedTypes(type))
				if (referenced.Namespace?.StartsWith("EtcdTerminal.Infrastructure", StringComparison.Ordinal) is true)
					violations.Add($"{type.FullName} -> {referenced.FullName}");
		}

		Assert.That(violations, Is.Empty);
	}

	[Test]
	public void ComponentsAndEngineDoNotReferenceScreens()
	{
		var appAssembly = typeof(App.Screens.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			var ns = type.Namespace;

			var isComponentOrEngine = ns?.StartsWith("EtcdTerminal.App.Components", StringComparison.Ordinal) is true
				|| ns?.StartsWith("EtcdTerminal.App.Engine", StringComparison.Ordinal) is true;

			if (!isComponentOrEngine)
				continue;

			foreach (var referenced in GetReferencedTypes(type))
				if (referenced.Namespace?.StartsWith("EtcdTerminal.App.Screens", StringComparison.Ordinal) is true)
					violations.Add($"{type.FullName} -> {referenced.FullName}");
		}

		Assert.That(violations, Is.Empty);
	}

	[Test]
	public void OnlySetupReferencesInfrastructure()
	{
		var appAssembly = typeof(App.Screens.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			if (type.Namespace?.StartsWith("EtcdTerminal.App", StringComparison.Ordinal) is not true)
				continue;

			if (type.Namespace?.StartsWith("EtcdTerminal.App.Setup", StringComparison.Ordinal) is true)
				continue;

			if (_promptException.Contains(type.FullName))
				continue;

			foreach (var referenced in GetReferencedTypes(type))
				if (referenced.Namespace?.StartsWith("EtcdTerminal.Infrastructure", StringComparison.Ordinal) is true)
					violations.Add($"{type.FullName} -> {referenced.FullName}");
		}

		Assert.That(violations, Is.Empty);
	}

	[Test]
	public void DomainDoesNotReferenceInfrastructure()
	{
		var domainAssembly = typeof(Terminal.ITerminal).Assembly;

		var infrastructureRefs = domainAssembly.GetReferencedAssemblies()
			.Where(a => a.Name == "EtcdTerminal.Infrastructure")
			.Select(a => a.FullName)
			.ToList();

		Assert.That(infrastructureRefs, Is.Empty);
	}

	[Test]
	public void ScreensContainNoEscapeLiterals()
	{
		var screensDir = Path.Combine(FindRepoRoot(), "src", "EtcdTerminal.App", "Screens");

		var hits = Directory.GetFiles(screensDir, "*.cs", SearchOption.AllDirectories)
			.Where(f => File.ReadAllText(f).Contains("\\x1b"))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);

		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "EtcdTerminal.slnx")))
			dir = dir.Parent;

		if (dir is null)
			throw new InvalidOperationException("Could not locate repository root.");

		return dir.FullName;
	}

	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(t => t is not null).Cast<Type>();
		}
	}

	private static IEnumerable<Type> GetReferencedTypes(Type type)
	{
		var seen = new HashSet<Type>();
		var queue = new Queue<Type>(SeedReferences(type));

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();

			if (!seen.Add(current))
				continue;

			yield return current;

			if (current.IsGenericType)
				foreach (var arg in current.GetGenericArguments())
					queue.Enqueue(arg);

			var element = current.HasElementType ? current.GetElementType() : null;

			if (element is not null)
				queue.Enqueue(element);
		}

		static IEnumerable<Type> SeedReferences(Type type)
		{
			var seeds = new List<Type>();

			if (type.BaseType is not null)
				seeds.Add(type.BaseType);

			seeds.AddRange(type.GetInterfaces());

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				seeds.Add(field.FieldType);

			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				seeds.Add(property.PropertyType);

			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			{
				seeds.Add(method.ReturnType);
				seeds.AddRange(method.GetParameters().Select(p => p.ParameterType));
			}

			foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
				seeds.AddRange(ctor.GetParameters().Select(p => p.ParameterType));

			return seeds;
		}
	}

	private static bool IsSpectreType(Type type) =>
		type.Assembly.GetName().Name?.StartsWith("Spectre", StringComparison.Ordinal) is true;
}
