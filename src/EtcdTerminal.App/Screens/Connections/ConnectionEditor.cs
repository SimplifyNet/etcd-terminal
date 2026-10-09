using EtcdTerminal.App.Components;
using EtcdTerminal.Configuration;
using EtcdTerminal.Presentation.Localization;

namespace EtcdTerminal.App.Screens.Connections;

public sealed class ConnectionEditor(IConnectionConfigRepository _configRepo, UserInput _input, Message _message, ILocalizationCatalog _localizations)
{
	public void Add() => SaveInstanceInteractive(null, _localizations.Current.InstanceAdded);

	public void Edit(EtcdConnectionConfig existing) => SaveInstanceInteractive(existing, _localizations.Current.InstanceUpdated);

	private void SaveInstanceInteractive(EtcdConnectionConfig? existing, string successMessage)
	{
		var name = existing is null
			? _input.Ask(_localizations.Current.EnterInstanceName)
			: _input.Ask(_localizations.Current.EnterInstanceName, existing.Name);

		if (name is null)
			return;

		if (ConnectionNames.IsTaken(_configRepo.LoadInstances(), name, existing?.Name))
		{
			_message.ShowError(_localizations.Current.InstanceNameTaken);

			return;
		}

		var connectionString = _input.Ask(_localizations.Current.EnterConnStr, existing?.ConnectionString ?? _localizations.Current.DefaultConnStr);

		if (connectionString is null)
			return;

		if (!new EtcdConnectionConfig { ConnectionString = connectionString }.IsConnectionStringValid)
		{
			_message.ShowError(_localizations.Current.InvalidConnStr);

			return;
		}

		var username = existing is null
			? _input.Ask(_localizations.Current.EnterUsername, allowEmpty: true)
			: _input.Ask(_localizations.Current.EnterUsername, existing.Username ?? string.Empty);

		if (username is null)
			return;

		var password = AskPassword(existing, username);

		if (password is null)
			return;

		var config = new EtcdConnectionConfig
		{
			Name = name,
			ConnectionString = connectionString,
			Username = string.IsNullOrEmpty(username) ? null : username,
			Password = string.IsNullOrEmpty(password) ? null : password
		};

		if (existing is null)
			_configRepo.AddInstance(config);
		else
			_configRepo.UpdateInstance(existing.Name, config);

		_message.ShowSuccess(successMessage);
	}

	private string? AskPassword(EtcdConnectionConfig? existing, string username)
	{
		if (string.IsNullOrEmpty(username))
			return string.Empty;

		var password = existing?.Password ?? string.Empty;

		var passwordPrompt = existing is null
			? _localizations.Current.EnterPassword
			: _localizations.Current.EnterPasswordKeepCurrent;

		var entered = _input.Secret(passwordPrompt);

		if (entered is null)
			return null;

		if (existing is null || !string.IsNullOrEmpty(entered))
			password = entered;

		return password;
	}
}
