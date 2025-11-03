using System;
using UnityEngine.Scripting;

namespace SFramework.Core.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class SFInjectAttribute : PreserveAttribute
    {
    }
}