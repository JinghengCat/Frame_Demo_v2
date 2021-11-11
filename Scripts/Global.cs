using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global : MonoBehaviour
{
    private static Global m_Instance;
    public static Global Instance
    {
        get
        {
            return m_Instance;
        }
    }
    [SerializeField]
    private GlobalSetting m_Setting;

    public bool useAssetBundle
    {
        get
        {
#if UNITY_EDITOR
            return m_Setting.m_UseAssetBundle;
#else
            return true;
#endif

        }
    }

    private List<ManagerBase> m_ManagerList = new List<ManagerBase>();

    public UIManager UIManager;
    public ResourceManager ResourceManager;

    
    #region Flow
    private void RegisterManager()
    {
        ResourceManager = new ResourceManager();
        m_ManagerList.Add(ResourceManager);
        
        UIManager = new UIManager();
        m_ManagerList.Add(UIManager);
    }

    private void InitManager()
    {
        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnPreCreate();
        }

        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnCreate();
        }

        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnCreateFinish();
        }
    }

    private void ReleaseManager()
    {
        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnPreDestroy();
        }

        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnDestroy();
        }

        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnDestroyFinish();
        }

        m_ManagerList.Clear();
    }

    private void Awake()
    {
        m_Instance = this;
        RegisterManager();
        InitManager();
        Test();
    }

    private void Test()
    {
        System.Action<ResourceContainer> OnLoad = delegate (ResourceContainer container)
        {
            var asset = container.GetAsset<GameObject>("Cube");
            ResourceManager.GetInstance(container, asset);
            Debug.LogFormat("LoadFinished");
        };
        ResourceManager.LoadAsset("Entity/Cube.prefab", "entity.ab", typeof(GameObject), false, OnLoad);
    }

    private void OnDestroy()
    {
        ReleaseManager();
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnFixedUpdate(Time.deltaTime);
        }
    }

    private void Update()
    {
        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnUpdate(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < m_ManagerList.Count; i++)
        {
            m_ManagerList[i].OnLateUpdate(Time.deltaTime);
        }
    }
    #endregion
}
