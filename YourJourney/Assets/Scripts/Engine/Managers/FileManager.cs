using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// JSON serialization/deserialization for .jime editor files
/// </summary>
public class FileManager
{
	public Guid scenarioGUID { get; set; }
	public Guid campaignGUID { get; set; }
	public int loreStartValue { get; set; }
	public string specialInstructions { get; set; }
	public string fileVersion { get; set; }
	//public string fileName { get; set; }
	public string saveDate { get; set; }

	[JsonConverter( typeof( InteractionConverter ) )]
	public List<IInteraction> interactions { get; set; }
	public List<Trigger> triggers { get; set; }
	public List<Objective> objectives { get; set; }
	public List<TextBookData> resolutions { get; set; }
	public List<Threat> threats { get; set; }
	public List<Chapter> chapters { get; set; }
	public List<int> globalTiles { get; set; }
	public Dictionary<string, bool> scenarioEndStatus { get; set; }
	public TextBookData introBookData { get; set; }
	public ProjectType projectType { get; set; }
	public string scenarioName { get; set; }
	public string objectiveName { get; set; }
	public int threatMax { get; set; }
	public bool threatNotUsed { get; set; }
	public bool scenarioTypeJourney { get; set; }
	public int shadowFear { get; set; }
	public int loreReward { get; set; }
	public int xpReward { get; set; }

	public FileManager()
	{
		//empty ctor for json deserialization
	}

	public FileManager( Scenario source )
	{
		fileVersion = source.fileVersion;
		saveDate = source.saveDate;

		interactions = source.interactionObserver;
		triggers = source.triggersObserver.ToList();
		objectives = source.objectiveObserver.ToList();
		resolutions = source.resolutionObserver.ToList();
		threats = source.threatObserver.ToList();
		chapters = source.chapterObserver.ToList();
		globalTiles = source.globalTilePool.ToList();
		scenarioEndStatus = source.scenarioEndStatus;

		introBookData = source.introBookData;
		projectType = source.projectType;
		scenarioName = source.scenarioName;
		objectiveName = source.objectiveName;
		threatMax = source.threatMax;
		threatNotUsed = source.threatNotUsed;
		scenarioTypeJourney = source.scenarioTypeJourney;
		shadowFear = source.shadowFear;
		specialInstructions = source.specialInstructions;
		scenarioGUID = source.scenarioGUID;
		campaignGUID = source.campaignGUID;
		loreStartValue = source.loreStartValue;
		loreReward = source.loreReward;
		xpReward = source.xpReward;
	}

	/// <summary>
	/// Supply the FULL PATH with the filename
	/// </summary>
	public static Scenario LoadScenario( string filename )
	{
		try
		{
			string json = File.ReadAllText( filename );
			var fm = JsonConvert.DeserializeObject<FileManager>( json );

			return Scenario.CreateInstance( fm );
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Return ProjectItem info for Scenarios in Project folder
	/// </summary>
	public static IEnumerable<ProjectItem> GetProjects()
	{
		List<ProjectItem> items = new List<ProjectItem>();
		DirectoryInfo di = new DirectoryInfo( AppPaths.BaseFolder );
		//only .jime files - skips .DS_Store, desktop.ini, zips etc
		foreach ( FileInfo fi in di.GetFiles().Where( IsScenarioFile ) )
		{
			Scenario s = LoadScenario( fi.FullName );
			if ( s != null )
				items.Add( new ProjectItem()
				{
					Title = s.scenarioName,
					projectType = s.projectType,
					Date = s.saveDate,
					fileName = fi.Name,
					fileVersion = s.fileVersion
				} );
		}
		return items;
	}

	/// <summary>
	/// Return ProjectItem info for Campaigns in Project folder
	/// </summary>
	public static IEnumerable<ProjectItem> GetCampaigns()
	{
		string basePath = AppPaths.BaseFolder;

		List<ProjectItem> items = new List<ProjectItem>();
		DirectoryInfo di = new DirectoryInfo( basePath );
		//find campaigns
		foreach ( DirectoryInfo dInfo in di.GetDirectories() )
		{
			Campaign c = LoadCampaign( dInfo.Name );
			if ( c != null )
			{
				FileInfo fi = new FileInfo( Path.Combine( basePath, dInfo.Name, dInfo.Name + ".json" ) );
				ProjectItem pi = new ProjectItem();
				pi.projectType = ProjectType.Campaign;
				pi.Date = fi.LastWriteTime.ToString( "M/d/yyyy" );
				pi.Title = c.campaignName;
				pi.campaignDescription = c.description;
				pi.campaignGUID = dInfo.Name;
				pi.campaignStory = c.storyText;
				pi.fileVersion = c.fileVersion;
				pi.fileName = fi.FullName;
				items.Add( pi );
			}
		}

		return items;
	}

	public static Campaign LoadCampaign( string campaignGUID )
	{
		if ( campaignGUID == AppPaths.SavesFolderName )
			return null;

		try
		{
			string json = File.ReadAllText( Path.Combine( AppPaths.CampaignFolder( campaignGUID ), campaignGUID + ".json" ) );
			return JsonConvert.DeserializeObject<Campaign>( json );
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// unzip Campaigns into folders using GUID as name
	/// </summary>
	public static void UnpackCampaigns()
	{
		string basePath = AppPaths.BaseFolder;
		DirectoryInfo di = new DirectoryInfo( basePath );

		//zip files only, each one is handled on its own so a bad zip doesn't block the others
		foreach ( FileInfo fi in di.GetFiles().Where( x => x.Extension.Equals( ".zip", StringComparison.OrdinalIgnoreCase ) && !x.Name.StartsWith( "." ) ) )
		{
			try
			{
				using ( ZipArchive archive = ZipFile.OpenRead( fi.FullName ) )
				{
					var entries = archive.Entries.Where( x => !IsJunkZipEntry( x ) ).ToList();
					//the campaign's metadata file is named after its GUID
					var meta = entries.FirstOrDefault( x => x.Name.EndsWith( ".json", StringComparison.OrdinalIgnoreCase )
						&& Guid.TryParse( Path.GetFileNameWithoutExtension( x.Name ), out _ ) );
					if ( meta == null )
					{
						UnityEngine.Debug.Log( "UnpackCampaigns() SKIPPED (no campaign metadata): " + fi.Name );
						continue;
					}
					string campaignGUID = Path.GetFileNameWithoutExtension( meta.Name );
					string extractPath = AppPaths.CampaignFolder( campaignGUID );
					Directory.CreateDirectory( extractPath );

					//campaign packages are flat, so extract by file name only - this also handles zips
					//re-compressed by Finder/Explorer (files inside a sub folder) and blocks path traversal
					foreach ( ZipArchiveEntry entry in entries )
						entry.ExtractToFile( Path.Combine( extractPath, entry.Name ), true );
				}
			}
			catch ( Exception e )
			{
				UnityEngine.Debug.Log( "UnpackCampaigns() ERROR: " + fi.Name + ": " + e.Message );
			}
		}
	}

	/// <summary>
	/// folder entries and macOS metadata (__MACOSX/, ._resource forks, .DS_Store)
	/// </summary>
	static bool IsJunkZipEntry( ZipArchiveEntry entry )
	{
		string fullName = entry.FullName.Replace( '\\', '/' );
		return string.IsNullOrEmpty( entry.Name )
			|| fullName.StartsWith( "__MACOSX/" )
			|| entry.Name.StartsWith( "._" )
			|| entry.Name == ".DS_Store";
	}

	static bool IsScenarioFile( FileInfo fi )
	{
		return fi.Extension.Equals( ".jime", StringComparison.OrdinalIgnoreCase ) && !fi.Name.StartsWith( "._" );
	}

	/// <summary>
	/// full path to filename inside the "Your Journey" folder on any OS (see AppPaths)
	/// </summary>
	public static string GetFullPath( string filename )
	{
		return Path.Combine( AppPaths.BaseFolder, filename );
	}

	public static string GetFullPathWithCampaign( string filename, string campaignGUID )
	{
		return Path.Combine( AppPaths.CampaignFolder( campaignGUID ), filename );
	}
}
