using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command line / CI entry point:
///   Unity -batchmode -quit -projectPath YourJourney -executeMethod BuildScript.Build
///         -customBuildTarget StandaloneOSX -customBuildPath build/StandaloneOSX/YourJourney.app -buildVersion 1.0.0
/// customBuildTarget: StandaloneWindows64, StandaloneOSX, StandaloneLinux64 (defaults to the active target)
/// </summary>
public static class BuildScript
{
	public static void Build()
	{
		string[] args = Environment.GetCommandLineArgs();

		BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
		string targetArg = GetArg( args, "-customBuildTarget" );
		if ( !string.IsNullOrEmpty( targetArg ) )
			target = (BuildTarget)Enum.Parse( typeof( BuildTarget ), targetArg, true );

		string path = GetArg( args, "-customBuildPath" );
		if ( string.IsNullOrEmpty( path ) )
			path = Path.Combine( "build", target.ToString(), "YourJourney" );
		path = WithExtension( path, target );

		string version = GetArg( args, "-buildVersion" );
		if ( !string.IsNullOrEmpty( version ) && version != "none" )
			PlayerSettings.bundleVersion = version.TrimStart( 'v' );

		if ( target == BuildTarget.StandaloneOSX )
			SetMacArchitecture( "x64ARM64" );//one app for both Intel and Apple Silicon Macs

		var options = new BuildPlayerOptions
		{
			scenes = EditorBuildSettings.scenes.Where( s => s.enabled ).Select( s => s.path ).ToArray(),
			locationPathName = path,
			target = target,
			targetGroup = BuildPipeline.GetBuildTargetGroup( target ),
			options = BuildOptions.None
		};

		Debug.Log( $"BuildScript: building {target} {PlayerSettings.bundleVersion} to {path}" );
		BuildReport report = BuildPipeline.BuildPlayer( options );
		BuildSummary summary = report.summary;
		Debug.Log( $"BuildScript: {summary.result}, {summary.totalErrors} errors, {summary.totalSize} bytes, {summary.totalTime}" );

		if ( Application.isBatchMode )
			EditorApplication.Exit( summary.result == BuildResult.Succeeded ? 0 : 1 );
	}

	/// <summary>
	/// UserBuildSettings lives in the Mac build support module, so use reflection to
	/// keep this script compiling on editors without Mac build support installed
	/// </summary>
	static void SetMacArchitecture( string architecture )
	{
		Type settings = AppDomain.CurrentDomain.GetAssemblies()
			.Select( a => a.GetType( "UnityEditor.OSXStandalone.UserBuildSettings" ) )
			.FirstOrDefault( t => t != null );
		PropertyInfo prop = settings?.GetProperty( "architecture", BindingFlags.Public | BindingFlags.Static );
		if ( prop == null )
		{
			Debug.LogWarning( "BuildScript: Mac build support not found, using the default architecture" );
			return;
		}
		prop.SetValue( null, Enum.Parse( prop.PropertyType, architecture ) );
	}

	static string GetArg( string[] args, string name )
	{
		int i = Array.IndexOf( args, name );
		return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
	}

	static string WithExtension( string path, BuildTarget target )
	{
		switch ( target )
		{
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return path.EndsWith( ".exe" ) ? path : path + ".exe";
			case BuildTarget.StandaloneOSX:
				return path.EndsWith( ".app" ) ? path : path + ".app";
			case BuildTarget.StandaloneLinux64:
				return path.EndsWith( ".x86_64" ) ? path : path + ".x86_64";
			default:
				return path;
		}
	}
}
