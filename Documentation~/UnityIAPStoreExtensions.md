Store Extensions
================

Stores may offer unique functionality that does not fit into the normal cross-platform purchase flow. This extended functionality is accessed via the ``IExtensionProvider`` which is provided to your application when Unity IAP initializes successfully.

It is not necessary to use platform-dependent compilation when using extensions; each extension comes with a fake no-op implementation which is used when running on a platform that does not offer the extended functionality.

For example, the following snippet accesses the ``RefreshReceipt`` mechanism Apple offers to fetch a refreshed App Receipt from Apple's servers. It can be compiled on any Unity IAP platform, and if you were to run it on a non Apple platform such as Android it would have no effect; the supplied lambda would never be invoked.

````
/// <summary>
/// Called when Unity IAP is ready to make purchases.
/// </summary>
public void OnInitialized (IStoreController controller, IExtensionProvider extensions)
{
    extensions.GetExtension<IAppleExtensions> ().RefreshAppReceipt (result => {
        if (result) {
            // Refresh finished successfully.
        } else {
            // Refresh failed.
        }
    });
}
````

