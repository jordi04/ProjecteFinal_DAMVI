//using UnityEngine;
//using UnityEngine.Playables;

//[System.Serializable]
//public class ToggleKinematicClip : PlayableAsset
//{
//    public ExposedReference<GameObject> parentObject;

//    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
//    {
//        var playable = ScriptPlayable<ToggleKinematicBehaviour>.Create(graph);
//        var behaviour = playable.GetBehaviour();

//        // Resolve the parent GameObject from Timeline
//        GameObject parent = parentObject.Resolve(graph.GetResolver());
//        behaviour.Initialize(parent);

//        return playable;
//    }
//}
