using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(SelfPropagatingNode))]
public class SelfPropagatingNodeEditor : NodeEditor
{
    private SelfPropagatingNode simpleNode;

    private float timeSinceDirty;

    bool IsWaitingToRecalc = false;
    DateTime StartTime;

    public override void OnBodyGUI()
    {
        base.OnBodyGUI();
        Event e = Event.current;

        SelfPropagatingNode node = target as SelfPropagatingNode;

        int delta = (DateTime.Now - StartTime).Milliseconds;

        // This is a hack, because on validate gets called every time a character is entered, 
        // so for not wait half a second to see if they dirt the thing again and wait to calc until after that
        if (IsDirty)
        {
            StartTime = DateTime.Now;
            IsWaitingToRecalc = true;
        }
        else if(IsWaitingToRecalc && delta > 500)
        {
            //Debug.Log(delta);
            node.Propogate();
            IsWaitingToRecalc = false;
        }
    }
}
