package md5b6d2baa209cf36fbf66873dd577e41be;


public class OhmLaw
	extends android.app.Activity
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreate:(Landroid/os/Bundle;)V:GetOnCreate_Landroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("ElectronicHelper.OhmLaw, ElectronicHelper", OhmLaw.class, __md_methods);
	}


	public OhmLaw ()
	{
		super ();
		if (getClass () == OhmLaw.class)
			mono.android.TypeManager.Activate ("ElectronicHelper.OhmLaw, ElectronicHelper", "", this, new java.lang.Object[] {  });
	}


	public void onCreate (android.os.Bundle p0)
	{
		n_onCreate (p0);
	}

	private native void n_onCreate (android.os.Bundle p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
