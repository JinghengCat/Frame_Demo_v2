using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ManagerBase 
{
    public abstract void OnPreCreate();
   
    public abstract void OnCreate();

    public abstract void OnCreateFinish();

    public abstract void OnPreDestroy();

    public abstract void OnDestroy();

    public abstract void OnDestroyFinish();

    public virtual void OnFixedUpdate(float delta) { }
    
    public virtual void OnUpdate(float delta) { }

    public virtual void OnLateUpdate(float delta) { }
}
