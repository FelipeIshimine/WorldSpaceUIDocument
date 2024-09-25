using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetOnHover : MonoBehaviour
{
	public List<WorldSpaceUIDocument> targets = new List<WorldSpaceUIDocument>();

	public float zOffset;

	private float vel;

    void Update()
    {
	    if (targets.Exists(x => x.IsHovering))
	    {
		    var localPos = transform.localPosition;
		    localPos.z = Mathf.SmoothDamp(localPos.z, zOffset, ref vel, .1f);
		    transform.localPosition = localPos;
	    }
	    else
	    {
		    var localPos = transform.localPosition;
		    localPos.z = Mathf.SmoothDamp(localPos.z, 0, ref vel, .1f);
		    transform.localPosition = localPos;
	    }
    }
}
