using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EtcdTerminal.App.Setup;
using NUnit.Framework;
using Simplify.DI;

namespace EtcdTerminal.Tests;

[TestFixture]
public sealed class ArchitectureTests
{
	[OneTimeSetUp]
	public void RegisterApplication() => DIContainer.Current.RegisterAll();

	[Test]
	public void AppTypesDoNotReferenceSpectre()
	{
		var appAssembly = typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			if (type.Namespace?.StartsWith("EtcdTerminal.App.Setup", StringComparison.Ordinal) is true)
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
		var appAssembly = typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly;

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
		var appAssembly = typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly;

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
		var appAssembly = typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly;

		var violations = new List<string>();

		foreach (var type in SafeGetTypes(appAssembly))
		{
			if (type.Namespace?.StartsWith("EtcdTerminal.App", StringComparison.Ordinal) is not true)
				continue;

			if (type.Namespace?.StartsWith("EtcdTerminal.App.Setup", StringComparison.Ordinal) is true)
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
		var domainAssembly = typeof(EtcdOperationException).Assembly;

		Assert.That(domainAssembly.GetName().Name, Is.EqualTo("EtcdTerminal"));

		var infrastructureRefs = domainAssembly.GetReferencedAssemblies()
			.Where(a => a.Name == "EtcdTerminal.Infrastructure")
			.Select(a => a.FullName)
			.ToList();

		Assert.That(infrastructureRefs, Is.Empty);
	}

	[Test]
	public void DomainDoesNotReferencePresentation()
	{
		var domainAssembly = typeof(EtcdOperationException).Assembly;

		Assert.That(domainAssembly.GetName().Name, Is.EqualTo("EtcdTerminal"));

		var presentationRefs = domainAssembly.GetReferencedAssemblies()
			.Where(a => a.Name == "EtcdTerminal.Presentation")
			.Select(a => a.FullName)
			.ToList();

		Assert.That(presentationRefs, Is.Empty);
	}

	[Test]
	public void DomainSourcesContainNoUiContractNamespaces()
	{
		var hits = SourceFiles("EtcdTerminal")
			.Where(f => Regex.IsMatch(File.ReadAllText(f), @"^namespace EtcdTerminal\.(Terminal|Theming|Localization)\b", RegexOptions.Multiline))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void PresentationReferencesNoDomainInfrastructureOrSpectre()
	{
		var presentationAssembly = typeof(Presentation.Block).Assembly;

		var forbidden = new[] { "EtcdTerminal", "EtcdTerminal.Infrastructure", "EtcdTerminal.App" };

		var violations = presentationAssembly.GetReferencedAssemblies()
			.Where(a => forbidden.Contains(a.Name ?? string.Empty) || (a.Name?.StartsWith("Spectre", StringComparison.Ordinal) is true))
			.Select(a => a.FullName)
			.ToList();

		Assert.That(violations, Is.Empty);
	}

	[Test]
	public void AppSourcesContainNoSpectreConsoleReferences()
	{
		var hits = SourceFilesOutsideCompositionRoot("EtcdTerminal.App")
			.Where(f => File.ReadAllText(f).Contains("Spectre.Console"))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void AppSourcesContainNoEscapeLiterals()
	{
		var hits = SourceFiles("EtcdTerminal.App")
			.Where(f => File.ReadAllText(f).Contains("\\x1b"))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void AppSourcesContainNoSystemConsoleAccess()
	{
		var hits = SourceFilesOutsideCompositionRoot("EtcdTerminal.App")
			.Where(f => Regex.IsMatch(File.ReadAllText(f), @"\bConsole\."))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void InfrastructureDoesNotReferenceApp()
	{
		var hits = SourceFiles("EtcdTerminal.Infrastructure")
			.Where(f => File.ReadAllText(f).Contains("using EtcdTerminal.App"))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void PresentationSourcesContainNoSpectre()
	{
		var hits = SourceFiles("EtcdTerminal.Presentation")
			.Where(f => File.ReadAllText(f).Contains("Spectre"))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void PresentationSourcesContainNoEscapeSequencesOrConsoleAccess()
	{
		var hits = SourceFiles("EtcdTerminal.Presentation")
			.Where(f =>
			{
				var text = File.ReadAllText(f);

				return text.Contains("\\x1b") || Regex.IsMatch(text, @"\bConsole\.");
			})
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void AppAndPresentationSourcesContainNoGeometryApis()
	{
		var forbidden = new[] { "CursorTop", "CursorLeft", "SetCursorPosition", "WindowWidth", "WindowHeight", "DisplayCells", "SelectionPointer", "new string(' '" };
		var hits = SourceFiles("EtcdTerminal.App")
			.Concat(SourceFiles("EtcdTerminal.Presentation"))
			.Where(f =>
			{
				var text = File.ReadAllText(f);

				return forbidden.Any(s => text.Contains(s));
			})
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void InfrastructureEscapeSequencesLiveOnlyInTerminalSession()
	{
		var hits = SourceFiles("EtcdTerminal.Infrastructure")
			.Where(f => Path.GetFileName(f) != "ConsoleTerminalSession.cs")
			.Where(f =>
			{
				var text = File.ReadAllText(f);

				return text.Contains("\\x1b") || text.Contains("\\u001b");
			})
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void SystemConsoleIsUsedOnlyByTerminalSession()
	{
		var hits = SourceFiles("EtcdTerminal.Infrastructure")
			.Where(f => Path.GetFileName(f) != "ConsoleTerminalSession.cs")
			.Where(f => Regex.IsMatch(File.ReadAllText(f), @"(?<!Spectre\.)\bConsole\."))
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.Empty);
	}

	[Test]
	public void InfrastructureSpaceFillLivesOnlyInBackgroundBand()
	{
		var hits = SourceFiles("EtcdTerminal.Infrastructure")
			.Where(f =>
			{
				var text = File.ReadAllText(f);

				return text.Contains("Segment.CellCount") || text.Contains("new string(' '");
			})
			.Select(Path.GetFileName)
			.ToList();

		Assert.That(hits, Is.EquivalentTo(new[] { "BackgroundBand.cs" }));
	}

	[Test]
	public void EveryMainMenuEntryIsRegistered()
	{
		var resolved = DIContainer.Current.Resolve<IEnumerable<App.Screens.MainMenu.IMainMenuEntry>>()
			.Select(e => e.GetType())
			.ToList();
		var declared = SafeGetTypes(typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly)
			.Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(App.Screens.MainMenu.IMainMenuEntry).IsAssignableFrom(t))
			.ToList();

		Assert.That(resolved, Is.EquivalentTo(declared));
	}

	[Test]
	public void EveryRoleCommandIsRegistered()
	{
		var commands = DIContainer.Current.Resolve<IEnumerable<App.Components.IMenuCommand<App.Screens.Roles.RoleMenuAction>>>()
			.ToList();
		var declared = SafeGetTypes(typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly)
			.Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(App.Components.IMenuCommand<App.Screens.Roles.RoleMenuAction>).IsAssignableFrom(t))
			.ToList();

		Assert.Multiple(() =>
		{
			Assert.That(commands.Select(c => c.GetType()), Is.EquivalentTo(declared));
			Assert.That(commands.Select(c => c.Action), Is.EquivalentTo(Enum.GetValues<App.Screens.Roles.RoleMenuAction>()));
		});
	}

	[Test]
	public void EveryUserCommandIsRegistered()
	{
		var commands = DIContainer.Current.Resolve<IEnumerable<App.Components.IMenuCommand<App.Screens.Users.UserMenuAction>>>()
			.ToList();
		var declared = SafeGetTypes(typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly)
			.Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(App.Components.IMenuCommand<App.Screens.Users.UserMenuAction>).IsAssignableFrom(t))
			.ToList();

		Assert.Multiple(() =>
		{
			Assert.That(commands.Select(c => c.GetType()), Is.EquivalentTo(declared));
			Assert.That(commands.Select(c => c.Action), Is.EquivalentTo(Enum.GetValues<App.Screens.Users.UserMenuAction>()));
		});
	}

	[Test]
	public void ConstructorsTakeAtMostFourParameters()
	{
		var assemblies = new[]
		{
			typeof(App.Screens.Connections.InstanceSelectionScreen).Assembly,
			typeof(Infrastructure.Terminal.BackgroundBand).Assembly
		};

		var violations = new List<string>();

		foreach (var type in assemblies.SelectMany(SafeGetTypes))
		{
			if (!type.IsClass || type.IsAbstract)
				continue;

			if (type.IsDefined(typeof(CompilerGeneratedAttribute)) || type.Name.Contains('<'))
				continue;

			if (type.GetMethod("<Clone>$") is not null)
				continue;

			if (type.Namespace?.StartsWith("EtcdTerminal.App.Setup", StringComparison.Ordinal) is true)
				continue;

			foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
			{
				var count = ctor.GetParameters().Length;

				if (count > 4)
					violations.Add($"{type.FullName}: {count}");
			}
		}

		Assert.That(violations, Is.Empty);
	}

	/// <summary>
	/// Source files outside <c>Setup</c>. The composition root is the one App
	/// file allowed to name Infrastructure and Spectre types while wiring them.
	/// </summary>
	private static IEnumerable<string> SourceFilesOutsideCompositionRoot(string project) =>
		SourceFiles(project)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Setup{Path.DirectorySeparatorChar}"));

	private static IEnumerable<string> SourceFiles(string project)
	{
		var dir = Path.Combine(FindRepoRoot(), "src", project);

		return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
			.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
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
