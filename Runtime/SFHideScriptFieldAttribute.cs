using System;
using UnityEngine.Scripting;

namespace SFramework.Core.Runtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SFHideScriptFieldAttribute : PreserveAttribute { }
}