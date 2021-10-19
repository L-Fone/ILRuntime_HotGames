﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UHotAssetBundleLoader : AHotBase
{
	protected override bool bCanBeAutoClosed => false;
	private static UHotAssetBundleLoader sinstance;
	public static UHotAssetBundleLoader Instance
	{
		get
		{
			if (sinstance == null)
			{
				var obj = new GameObject("UHotAssetBundleLoader");
				sinstance = new UHotAssetBundleLoader();
				sinstance.SetGameObj(obj, "");
			}
			return sinstance;
		}
	}
	public T OnLoadAsset<T>(string path) where T : UnityEngine.Object
	{
		if (!HotEnvironment.UseAB && HotEnvironment.IsEditor)
		{
			return Utils.LoadAsset<T>(path);
		}
		else
		{
			var deps = new List<string>();
			OnGetAssetBundleDependeces(path + AssetBundleSuffix, deps);
			foreach (var dep in deps)
			{
				OnGetAssetBundle(dep);
			}
			var asset = OnGetAssetBundle(path);
			if (asset == null)
			{
				return null;
			}
			return asset.LoadAsset<T>(Utils.GetAssetBundleName(path));
		}
	}
	private AssetBundle DoGetAssetBundle(string assetBundlePath)
	{
		if (dAssetBundles.ContainsKey(assetBundlePath))
		{
			return dAssetBundles[assetBundlePath];
		}
		var path = Utils.ConfigSaveDir + "/" + assetBundlePath;
		if (!File.Exists(path))
		{
			return null;
		}
		var ab = AssetBundle.LoadFromFile(path);
		if (ab == null)
		{
			return null;
		}
		dAssetBundles.Add(assetBundlePath, ab);
		var deps = new List<string>();
		OnGetAssetBundleDependeces(assetBundlePath, deps);
		foreach (var d in deps)
		{
			OnGetAssetBundle(d);
		}
		return ab;
	}
	public static string AssetBundleSuffix = ".ab";
	public AssetBundle OnGetAssetBundle(string assetBundlePath, bool NoDependences = false)
	{
		var platform = Utils.GetPlatformFolder();
		if (!assetBundlePath.EndsWith(AssetBundleSuffix))
		{
			assetBundlePath += AssetBundleSuffix;
		}
		assetBundlePath = assetBundlePath.ToLower();
		if (!NoDependences && dAssetBundles.ContainsKey(platform))
		{
			if (manifestBundle == null)
			{
				manifestBundle = dAssetBundles[platform];
			}
			if (manifest == null)
			{
				manifest = (AssetBundleManifest)manifestBundle.LoadAsset("AssetBundleManifest");
			}
		}
		if (dAssetBundles.ContainsKey(assetBundlePath))
		{
			return dAssetBundles[assetBundlePath];
		}
		return DoGetAssetBundle(assetBundlePath);
	}
	AssetBundle manifestBundle;
	AssetBundleManifest manifest;
	public void OnGetAssetBundleDependeces(string name, List<string> dependens)
	{
		name = name.StartsWith("/") ? name.Substring(1) : name;
		var platform = Utils.GetPlatformFolder();
		if (!dAssetBundles.ContainsKey(platform))
		{
			var ab = DoGetAssetBundle(platform);
			if (ab == null)
			{
				return;
			}
		}
		if (manifestBundle == null)
		{
			manifestBundle = dAssetBundles[platform];
		}
		if (manifest == null)
		{
			manifest = (AssetBundleManifest)manifestBundle.LoadAsset("AssetBundleManifest");
		}
		if (!dependens.Contains(name))
		{
			dependens.Add(name);
		}
		var result = manifest.GetAllDependencies(name);
		foreach (var r in result)
		{
			if (dependens.Contains(r))
			{
				continue;
			}
			OnGetAssetBundleDependeces(r, dependens);
		}
	}
	private Dictionary<string, AssetBundle> dAssetBundles = new Dictionary<string, AssetBundle>();
	private List<string> lDownloaded = new List<string>();
	Dictionary<string, string> dRemoteVersions = new Dictionary<string, string>();
	public void OnDownloadResources(Action downloaded, params string[] resources)
	{
		OnDownloadResources(resources.ToList(), downloaded);
	}
	public void OnDownloadResources(List<string> lResources, Action downloaded, Action<float> progress = null
		, bool checksuffix = true)
	{
		if (!HotEnvironment.UseAB)
		{
			downloaded?.Invoke();
			return;
		}
		if (dRemoteVersions.Count == 0)
		{
			OnDownloadText(Utils.GetPlatformFolder() + "/versions", (content) =>
			  {
				  var acontent = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				  foreach (var c in acontent)
				  {
					  var ac = c.Split('|');
					  if (ac.Length < 2)
					  {
						  continue;
					  }
					  if (!dRemoteVersions.ContainsKey(ac[0].ToLower()))
					  {
						  dRemoteVersions.Add(ac[0].ToLower(), ac[1]);
					  }
				  }
				  DoCheckVersions(lResources, downloaded, progress, checksuffix);
			  });
		}
		else
		{
			DoCheckVersions(lResources, downloaded, progress, checksuffix);
		}
	}
	private void DoCheckVersions(List<string> lResources, Action downloaded, Action<float> progress, bool checksuffix = true)
	{
		var lNeedDownload = new List<string>();
		foreach (var r in lResources)
		{
			var res = r;
			if (!res.StartsWith("/"))
			{
				res = $"/{res}";
			}
			if (checksuffix && !res.EndsWith(UHotAssetBundleLoader.AssetBundleSuffix))
			{
				res = res.ToLower();
				res = $"{res}{AssetBundleSuffix}";
			}
			if (!dRemoteVersions.ContainsKey(res.ToLower()))
			{
				continue;
			}
			var file = ULocalFileManager.Instance.OnGetFile(res);
			if (file == null || file.version != dRemoteVersions[res.ToLower()])
			{
				lNeedDownload.Add(res);
			}

			if (res.EndsWith(AssetBundleSuffix))
			{
				var deps = new List<string>();
				OnGetAssetBundleDependeces(res, deps);
				foreach (var dep in deps)
				{
					var rdep = dep;
					if (!dep.StartsWith("/"))
					{
						rdep = $"/{dep}";
					}
					if (!lNeedDownload.Contains(rdep))
					{
						if (!dRemoteVersions.ContainsKey(rdep.ToLower()))
						{
							continue;
						}
						file = ULocalFileManager.Instance.OnGetFile(rdep);
						if (file == null || file.version != dRemoteVersions[rdep.ToLower()])
						{
							lNeedDownload.Add(rdep.ToLower());
						}
					}
				}
			}
		}
		totalCount = lNeedDownload.Count;
		DoDownloadResources(lNeedDownload, downloaded, progress);
	}
	int totalCount;
	private void DoDownloadResources(List<string> lWaitingForDownload, Action downloaded, Action<float> progress)
	{
		if (lWaitingForDownload.Count > 0)
		{
			var resource = "";
			if (lWaitingForDownload.Count == 0)
			{
				resource = "";
			}
			else
			{
				resource = lWaitingForDownload[0];
				lWaitingForDownload.RemoveAt(0);
			}

			if (!string.IsNullOrEmpty(resource))
			{
				WWW w = OnDownloadBytes(resource
					, dRemoteVersions[resource.ToLower()]
					, (res) =>
					{
						lDownloaded.Add(res);
						DoDownloadResources(lWaitingForDownload, downloaded, progress);
					}
					, (err) =>
					{
						AOutput.Log($"Download {resource} failed:{err}");
						DoDownloadResources(lWaitingForDownload, downloaded, progress);
					}
					, (p) =>
					{
						var fp = (float)lDownloaded.Count / totalCount + p / totalCount;
						if (fp > fProgress)
						{
							fProgress = fp;
						}
						if (progress != null)
						{
							progress(fProgress);
						}
						else
						{
							UILoading.Instance?.OnSetProgress(fProgress);
						}
					});
				return;
			}
		}
		totalCount = 0;
		lDownloaded.Clear();
		fProgress = -1;
		progress?.Invoke(1);
		downloaded?.Invoke();
	}
	public float fProgress = -1;
	private WWW OnDownloadBytes(string resource
		 , string version
		 , Action<string> downloadedAction
		 , Action<string> errorAction = null
		 , Action<float> progressAction = null
	 )
	{
		if (!HotEnvironment.UseAB)
		{
			return null;
		}
		var url = Utils.BaseURL_Res + Utils.GetPlatformFolder() + resource;
		var www = new WWW(url);
		addUpdateAction(() =>
		{
			if (www.isDone)
			{
				progressAction?.Invoke(1);
				if (string.IsNullOrEmpty(www.error))
				{
					var filepath = Utils.ConfigSaveDir + resource;
					var fi = new FileInfo(filepath);
					if (!fi.Directory.Exists)
					{
						fi.Directory.Create();
					}
					File.WriteAllBytes(filepath, www.bytes);
					ULocalFileManager.Instance.OnAddFile(resource, version);
					downloadedAction?.Invoke(resource);
				}
				else
				{
					UDebugHotLog.Log($"{url} error {www.error}");
					errorAction?.Invoke(www.error);
				}
				return true;
			}
			else
			{
				if (progressAction != null)
					progressAction(www.progress);
				else
				{
					UILoading.Instance?.OnSetProgress(www.progress);
				}
			}
			return false;
		});
		return www;
	}
	public WWW OnDownloadText(string resource, Action<string> downloadedAction, Action<string> errorAction = null)
	{
		if (!HotEnvironment.UseAB)
		{
			return null;
		}
		var url = Utils.BaseURL_Res + resource + $".txt?{ApiDateTime.SecondsFromBegin()}";
		var www = new WWW(url);
		addUpdateAction(() =>
		{
			if (www.isDone)
			{
				if (string.IsNullOrEmpty(www.error))
				{
					lDownloaded.Add(resource);
					downloadedAction?.Invoke(www.text);
				}
				else
				{
					UDebugHotLog.Log($"OnDownloadText {www.url} error {www.error}");
					errorAction?.Invoke(www.error);
				}
				return true;
			}
			return false;
		});
		return www;
	}
	protected override void InitComponents() { }
}
