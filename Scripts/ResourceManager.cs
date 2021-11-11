using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;


public class Task
{
    public string bundlePath;
    public string assetPath;
    public bool onlyLoadBundle;
    public bool needAutoRelease;
    public Type assetType;
    public Action<ResourceContainer> OnLoadFinish;
}

public class BundleData
{
    public string bundleName;
    public BundleData[] dependencies;
}


public class ResourceManager : ManagerBase
{
    private Dictionary<string, ResourceContainer> m_ResourceDict = new Dictionary<string, ResourceContainer>();
    private string m_ResourceRootPath;

    private int m_Worker = 12;
    private List<Coroutine> m_WorkCo = new List<Coroutine>();
    private Queue<Task> m_WorkTaskQueue = new Queue<Task>();

    private Dictionary<string, BundleData> m_BundleDatas = new Dictionary<string, BundleData>();
    private string m_BundlePath = string.Format("{0}/../Build", Application.dataPath);
    public bool useAssetBundle
    {
        get
        {
            return Global.Instance.useAssetBundle;
        }
    }

    #region Flow
    public override void OnPreCreate()
    {
    }

    public override void OnCreate()
    {
        OnInitResourcesPath();
        OnInitWorkFlow();
        if (useAssetBundle)
        {
            OnInitBundlesDependencies();
            Test_PrintBundleInfo();
        }
    }

    public override void OnCreateFinish()
    {
    }

    public override void OnPreDestroy()
    {
    }

    public override void OnDestroy()
    {
    }

    public override void OnDestroyFinish()
    {
    }
    #endregion


    private void OnInitResourcesPath()
    {
        m_ResourceRootPath = string.Format("{0}/{1}/", Application.dataPath, "AssetBundle");
        //Debug.Log("Init ResourceRootPath = " + m_ResourceRootPath);
    }

    private void OnInitWorkFlow()
    {
        for (int i = 0; i < m_Worker; i++)
        {
            m_WorkCo.Add(Global.Instance.StartCoroutine(DoTask()));
        }

        Global.Instance.StartCoroutine(AutoReleaseResource());
    }

    private void Test_PrintBundleInfo()
    {
        foreach (var item in m_BundleDatas)
        {
            var bundleName = item.Key;
            var bundleData = item.Value;
            var bundleDeps = bundleData.dependencies;
            string deps = "";
            for (int i = 0; i < bundleDeps.Length; i++)
            {
                deps += bundleDeps[i].bundleName+", ";
            }
            Debug.LogFormat("BundleName = {0}, BundleDeps = {1}", bundleName, deps);
        }
    }

    private void OnInitBundlesDependencies()
    {
        var manifestBundle = AssetBundle.LoadFromFile(string.Format("{0}/Build", m_BundlePath));
        AssetBundleManifest manifest = (AssetBundleManifest)manifestBundle.LoadAsset("AssetBundleManifest");
        var allBundle = manifest.GetAllAssetBundles();
        for (int i = 0; i < allBundle.Length; i++)
        {
            GetBundleData(manifest, allBundle[i]);
        }
    }

    private BundleData GetBundleData(AssetBundleManifest manifest, string bundleName)
    {
        BundleData bundleData = null;
        if (!m_BundleDatas.TryGetValue(bundleName, out bundleData))
        {
            bundleData = new BundleData();
            var deps = manifest.GetAllDependencies(bundleName);
            bundleData.bundleName = bundleName;
            bundleData.dependencies = new BundleData[deps.Length];

            m_BundleDatas.Add(bundleName, bundleData);

            int depCount = bundleData.dependencies.Length;
            for (int i = 0; i < depCount; i++)
            {
                bundleData.dependencies[i] = GetBundleData(manifest, deps[i]);
            }
        }


        return bundleData;
    }

    private IEnumerator DoTask()
    {
        while (true)
        {
            if (m_WorkTaskQueue.Count > 0)
            {
                var task = m_WorkTaskQueue.Dequeue();
                var bundlePath = task.bundlePath;
                m_ResourceDict.TryGetValue(task.bundlePath, out var container);
                var count = 0;
                while (!CheckDepsAllLoaded(container))
                {
                    count++;
                    if (count >= 1000)
                    {
                        break;
                    }
                    //当其依赖还没加载完是，将协程卡住
                    yield return null;
                }
#if UNITY_EDITOR
                if (useAssetBundle)
                {
                    Global.Instance.StartCoroutine(LoadAssetInAssetBundleAsync(task));
                }
                else
                {
                    LoadAssetInEditor(task);
                }
#else
                LoadAssetInAssetBundleAsync(task);
#endif
            }
            yield return null;
        }

    }

    private IEnumerator AutoReleaseResource()
    {
        //因为foreach中不能移除元素所以添加一个移除的List
        var list = new List<string>();
        while (true)
        {
            list.Clear();
            foreach (var item in m_ResourceDict)
            {
                var container = item.Value;
                //Debug.LogFormat("Container = {0}, LeftSecToRelease = {1}, RefCount = {2}, LoadState = {3}", container.bundlePath, container.LeftSecToRelease, container.RefCount, container.loadState);
                if (container.loadState == LoadState.Loaded && container.RefCount <= 0 && container.autoRelease)
                {
                    container.LeftSecToRelease -= 1;

                    if (container.LeftSecToRelease <= 0)
                    {
                        list.Add(item.Key);
                        //Debug.LogFormat("{0} Add To UnLoad List", item.Key);
                    }
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                UnLoadContainer(m_ResourceDict[list[i]]);
                m_ResourceDict.Remove(list[i]);
            }


            yield return new WaitForSeconds(1);
        }
    }

    

    /// <summary>
    /// 在编辑器和设备上都调用此接口
    /// </summary>
    /// <param name="assetPath">相对于AssetBundle文件夹下的路径</param>
    /// <param name="bundlePath">相对于Assets/AssetBundle下的路径</param>
    /// <param name="assetType"></param>
    /// <param name="isAsync"></param>
    /// <param name="OnLoad"></param>
    public ResourceContainer LoadAsset(string assetPath, string bundlePath, Type assetType, bool isAsync, Action<ResourceContainer> OnLoad)
    {
#if UNITY_EDITOR
        if (useAssetBundle)
        {
            return LoadAssetImp(assetPath, bundlePath, assetType, isAsync, OnLoad);
        }
        else
        {
            return LoadAssetImp(assetPath, assetPath, assetType, isAsync, OnLoad);
        }
#else
        return LoadAssetImp(assetPath, bundlePath, assetType, isAsync, OnLoad);
#endif
    }

    public void UnLoadContainer(ResourceContainer container)
    {
        container.loadState = LoadState.UnLoaded;
        
        if (!useAssetBundle)
        {
            Debug.LogFormat("Release Container {0}", container.bundlePath);
            return;
        }
        else
        {
            Debug.LogFormat("Release Container {0}", container.bundlePath);
            container.bundle.Unload(false);
            container = null;
        }
    }
    
    public ResourceContainer LoadAssetImp(string assetPath, string bundlePath, Type assetType, bool isAsync,  Action<ResourceContainer> OnLoad, bool onlyLoadBundle = false, bool needAutoRelease = true)
    {
        if (string.IsNullOrEmpty(bundlePath))
        {
            Debug.LogError("BundlePath Cant be Empty Or Null");
            return null;
        }

        ResourceContainer container = null;
        if (!m_ResourceDict.TryGetValue(bundlePath, out container))
        {
            container = new ResourceContainer();
            m_ResourceDict.Add(bundlePath, container);
            container.bundlePath = bundlePath;

            if (useAssetBundle)
            {
                //加载依赖，走相同流程
                var bundleDeps = GetAllBundleDependencies(bundlePath);
                container.Dependencies = new ResourceContainer[bundleDeps.Length];
                for (int i = 0; i < bundleDeps.Length; i++)
                {
                    container.Dependencies[i] = LoadAsset(null, bundleDeps[i].bundleName, null, isAsync, null);
                }
            }
        }
        else
        {
            container.ResetContainer();
        }
        
        if (container.loadState == LoadState.Loaded)
        {
            OnLoad(container);
            return container;
        }
        
        container.loadState = LoadState.Loading;

        Task task = new Task();
        task.assetPath = assetPath;
        task.bundlePath = bundlePath;
        task.assetType = assetType;
        task.OnLoadFinish = OnLoad;
        task.onlyLoadBundle = onlyLoadBundle;
        task.needAutoRelease = needAutoRelease;

        if (isAsync)
        {
            m_WorkTaskQueue.Enqueue(task);
        }
        else
        {
            if (useAssetBundle)
            {
                LoadAssetBundle(task);
            }
            else
            {
                LoadAssetInEditor(task);
            }
        }

        return container;
    }
    private void LoadAssetInEditor(Task task)
    {
        if (m_ResourceDict.TryGetValue(task.bundlePath, out var container))
        {
            container.loadState = LoadState.Loaded;
        }

        task.OnLoadFinish?.Invoke(container);
    }

    private void LoadAssetBundle(Task task)
    {
        var path = string.Format("{0}/{1}", m_BundlePath, task.bundlePath);
        var bundle = AssetBundle.LoadFromFile(path);
        if (m_ResourceDict.TryGetValue(task.bundlePath, out var container))
        {
            var count = 0;
            while (!CheckDepsAllLoaded(container))
            {
                count++;
                if (count >= 1000)
                {
                    Debug.LogErrorFormat("Dead Loop");
                    break;
                }
            }
            
            container.loadState = LoadState.Loaded;
            container.bundle = bundle;
        }

        task.OnLoadFinish?.Invoke(container);
    }

    private IEnumerator LoadAssetInAssetBundleAsync(Task task)
    {

        var path = string.Format("{0}/{1}", m_BundlePath, task.bundlePath);
        //Debug.LogFormat("LoadAssetInAssetBundleAsync Path = {0}", path);
        var request = AssetBundle.LoadFromFileAsync(path);
        yield return request;

        if (m_ResourceDict.TryGetValue(task.bundlePath, out var container))
        {
            container.loadState = LoadState.Loaded;
            container.bundle = request.assetBundle;
            //Debug.LogFormat("{0} LoadFinished", task.bundlePath);
        }

        task.OnLoadFinish?.Invoke(container);
    }

    private BundleData[] GetAllBundleDependencies(string bundleName)
    {
        BundleData bundleData;
        if (!m_BundleDatas.TryGetValue(bundleName, out bundleData))
        {
            Debug.LogErrorFormat("Do not have this bundle, check bundleName");
            return null;
        }
        return bundleData.dependencies;
    }

    private bool CheckDepsAllLoaded(ResourceContainer container)
    {
        if (container.Dependencies == null || container.Dependencies.Length == 0)
        {
            //Debug.LogFormat("{0}: All Deps Loaded ", container.bundlePath);
            return true;
        }

        foreach (var item in container.Dependencies)
        {
            if (item == null || item.loadState != LoadState.Loaded)
            {
                return false;
            }
        }

        return true;
    }

    public GameObject GetInstance(ResourceContainer container, GameObject asset)
    {
        var obj = GameObject.Instantiate<GameObject>(asset);
        obj.AddComponent<ResourceObj>().OnInit(container);
        return obj;
    }


}
