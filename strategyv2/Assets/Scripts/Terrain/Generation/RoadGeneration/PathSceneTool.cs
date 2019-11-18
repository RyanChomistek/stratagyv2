using UnityEngine;

using PathCreation;

public abstract class PathSceneTool : MonoBehaviour
{
    public event System.Action onDestroyed;
    public PathCreator pathCreator;
    public bool autoUpdate = true;

    protected VertexPath path {
        get {
            return pathCreator.path;
        }
    }

    public void TriggerUpdate() {
        PathUpdated();
    }


    public virtual void OnDestroy() {
        if (onDestroyed != null) {
            onDestroyed();
        }
    }

    protected abstract void PathUpdated();
}

