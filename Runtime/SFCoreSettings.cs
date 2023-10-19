﻿using System;

namespace SFramework.Core.Runtime
{
    [Serializable]
    public class SFCoreSettings : SFProjectSettings<SFCoreSettings>
    {
        public string GeneratorScriptsPath = "SFramework/Generated";
        public bool IsDebug;
    }
}