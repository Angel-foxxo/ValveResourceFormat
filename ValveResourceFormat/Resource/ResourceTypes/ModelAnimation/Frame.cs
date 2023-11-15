using System;
using System.Numerics;

namespace ValveResourceFormat.ResourceTypes.ModelAnimation
{
    public class Frame
    {
        public int FrameIndex { get; set; } = 1;
        public FrameBone[] Bones { get; }

        public Frame(Skeleton skeleton)
        {
            Bones = new FrameBone[skeleton.Bones.Length];
            Clear(skeleton);
        }

        public void SetAttribute(int bone, AnimationChannelAttribute attribute, object data)
        {
            if (data is Vector3 vector3)
            {
                SetAttribute(bone, attribute, vector3);
            }
            else if (data is Quaternion quaternion)
            {
                SetAttribute(bone, attribute, quaternion);
            }
            else if (data is float fl)
            {
                SetAttribute(bone, attribute, fl);
            }
            else
            {
                throw new ArgumentException("Unexpected data type");
            }
        }

        public void SetAttribute(int bone, AnimationChannelAttribute attribute, Vector3 data)
        {
            switch (attribute)
            {
                case AnimationChannelAttribute.Position:
                    Bones[bone].Position = data;
                    break;

#if DEBUG
                default:
                    Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Vector3 data");
                    break;
#endif
            }
        }

        public void SetAttribute(int bone, AnimationChannelAttribute attribute, Quaternion data)
        {
            switch (attribute)
            {
                case AnimationChannelAttribute.Angle:
                    Bones[bone].Angle = data;
                    break;

#if DEBUG
                default:
                    Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with Quaternion data");
                    break;
#endif
            }
        }

        public void SetAttribute(int bone, AnimationChannelAttribute attribute, float data)
        {
            switch (attribute)
            {
                case AnimationChannelAttribute.Scale:
                    Bones[bone].Scale = data;
                    break;

#if DEBUG
                default:
                    Console.WriteLine($"Unknown frame attribute '{attribute}' encountered with float data");
                    break;
#endif
            }
        }

        /// <summary>
        /// Resets frame bones to their bind pose.
        /// Should be used on animation change.
        /// </summary>
        /// <param name="skeleton">The same skeleton that was passed to the constructor.</param>
        public void Clear(Skeleton skeleton)
        {
            FrameIndex = -1;

            for (var i = 0; i < Bones.Length; i++)
            {
                Bones[i].Position = skeleton.Bones[i].Position;
                Bones[i].Angle = skeleton.Bones[i].Angle;
                Bones[i].Scale = 1;
            }
        }
    }
}
