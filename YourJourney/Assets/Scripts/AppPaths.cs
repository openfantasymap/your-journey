using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Resolves the "Your Journey" data folder the same way on every OS, so the
/// player and the JiME editor always read and write the same location:
///   Windows      %USERPROFILE%\Documents\Your Journey
///   macOS        ~/Documents/Your Journey
///   Linux        $XDG_DOCUMENTS_DIR/Your Journey (default ~/Documents/Your Journey)
///   Android/iOS  Application.persistentDataPath/Your Journey
/// Set the JIME_DATA_DIR environment variable to override the folder entirely.
/// </summary>
public static class AppPaths
{
	public const string FolderName = "Your Journey";
	public const string SavesFolderName = "Saves";
	public const string OverrideVariable = "JIME_DATA_DIR";

	static string baseFolder;

	/// <summary>
	/// Full path of the "Your Journey" folder, created if it doesn't exist
	/// </summary>
	public static string BaseFolder
	{
		get
		{
			if ( baseFolder == null )
				baseFolder = ResolveBaseFolder();
			if ( !Directory.Exists( baseFolder ) )
				Directory.CreateDirectory( baseFolder );
			return baseFolder;
		}
	}

	/// <summary>
	/// Full path of the Saves folder, created if it doesn't exist
	/// </summary>
	public static string SavesFolder
	{
		get
		{
			string path = Path.Combine( BaseFolder, SavesFolderName );
			if ( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );
			return path;
		}
	}

	public static string CampaignFolder( string campaignGUID )
	{
		return Path.Combine( BaseFolder, campaignGUID );
	}

	static string ResolveBaseFolder()
	{
		string custom = Environment.GetEnvironmentVariable( OverrideVariable );
		if ( !string.IsNullOrEmpty( custom ) )
			return custom;

		switch ( Application.platform )
		{
			case RuntimePlatform.Android:
			case RuntimePlatform.IPhonePlayer:
				return Path.Combine( Application.persistentDataPath, FolderName );
			case RuntimePlatform.WindowsPlayer:
			case RuntimePlatform.WindowsEditor:
				return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ), FolderName );
			default:
				return Path.Combine( UnixDocumentsFolder(), FolderName );
		}
	}

	/// <summary>
	/// Mono returns $HOME for SpecialFolder.MyDocuments on macOS/Linux, so build ~/Documents explicitly
	/// </summary>
	static string UnixDocumentsFolder()
	{
		string xdg = Environment.GetEnvironmentVariable( "XDG_DOCUMENTS_DIR" );
		if ( !string.IsNullOrEmpty( xdg ) )
			return xdg;

		string home = Environment.GetEnvironmentVariable( "HOME" );
		if ( string.IsNullOrEmpty( home ) )
			home = Environment.GetFolderPath( Environment.SpecialFolder.Personal );
		if ( string.IsNullOrEmpty( home ) )
			return Application.persistentDataPath;

		return Path.Combine( home, "Documents" );
	}
}
