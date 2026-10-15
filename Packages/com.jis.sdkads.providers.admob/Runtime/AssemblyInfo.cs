using UnityEngine.Scripting;

// JisAds loads this provider through reflection only (ProviderConfigFactory, *Bridge classes).
// Without a static reference the IL2CPP linker would drop the whole assembly from player builds.
[assembly: AlwaysLinkAssembly]
