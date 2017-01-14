using UnityEngine;
using System.Collections.Generic;

namespace BioIK {
	public class Chain {
		public Transform[] Segments;
		public KinematicJoint[] Joints;
		public float Length;
		public int DoF;

		public Chain(Transform start, Transform end) {
			List<Transform> segments = new List<Transform>();
			List<KinematicJoint> joints = new List<KinematicJoint>();
			Length = 0f;
			DoF = 0;

			Transform t = end;
			while(true) {
				segments.Add(t);
				KinematicJoint joint = t.GetComponent<KinematicJoint>();
				if(joint != null) {
					if(joint.GetDOF() != 0) {
						joints.Add(joint);
					}
				}
				if(t == start) {
					break;
				} else {
					t = t.parent;
				}
			} 
			
			segments.Reverse();
			joints.Reverse();
			Segments = segments.ToArray();
			Joints = joints.ToArray();

			if(Joints.Length == 0) {
				Length = 0f;
			} else {
				Vector3 reference = Joints[0].ComputeConnectionInWorldSpace();
				for(int i=1; i<Joints.Length; i++) {
					Length += Vector3.Distance(reference, Joints[i].ComputeConnectionInWorldSpace());
					reference = Joints[i].ComputeConnectionInWorldSpace();
				}
				Length += Vector3.Distance(reference, end.position);
			}

			for(int i=0; i<Joints.Length; i++) {
				DoF += Joints[i].GetDOF();
			}
		}
	}
}