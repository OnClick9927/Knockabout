using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"AOT.dll",
		"Google.Protobuf.dll",
		"IFramework.dll",
		"Luban.Runtime.dll",
		"System.Core.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"WooAsset.dll",
		"WooLocalization.dll",
		"WooTimer.dll",
		"WooTween.dll",
		"mscorlib.dll",
	};

}