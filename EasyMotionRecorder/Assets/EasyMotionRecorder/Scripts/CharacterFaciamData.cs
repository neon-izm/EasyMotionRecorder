using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Entum
{
	public class CharacterFaciamData : ScriptableObject
	{
      

		[System.SerializableAttribute]
		public class SerializeHumanoidFace
		{

			public class MeshAndBlendshape
			{
				public string path;
				public float[] blendShapes;
			}


			public int blendShapeNum()
			{
				return smeshes.Count == 0 ? 0 : smeshes.Sum(t => t.blendShapes.Length);
			}

			//フレーム数
			public int FrameCount;

			//記録開始後の経過時間。処理落ち対策
			public float Time;

			public SerializeHumanoidFace(SerializeHumanoidFace serializeHumanoidFace)
			{
				for (int i = 0; i < serializeHumanoidFace.smeshes.Count; i++)
				{
					smeshes.Add(serializeHumanoidFace.smeshes[i]);
					Array.Copy(serializeHumanoidFace.smeshes[i].blendShapes,smeshes[i].blendShapes,
						serializeHumanoidFace.smeshes[i].blendShapes.Length);

				}
				FrameCount = serializeHumanoidFace.FrameCount;
				Time = serializeHumanoidFace.Time;
			}
			//単一フレームの中でも、口のメッシュや目のメッシュなどが個別にここに入る
			public List<MeshAndBlendshape> smeshes= new List<MeshAndBlendshape>();
			public SerializeHumanoidFace()
			{
			}
		}

        
		public List<SerializeHumanoidFace> Facials = new List<SerializeHumanoidFace>();
	}
}