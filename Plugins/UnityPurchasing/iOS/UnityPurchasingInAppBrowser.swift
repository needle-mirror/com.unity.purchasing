import Foundation

// Matches the C# `delegate void InAppBrowserCallback(int reason)` and the
// previous Objective-C `typedef void (*InAppBrowserCallback)(int reason)`.
//   reason = 0  → UserDismissed
//   reason = 1  → Failed (no root view controller, invalid URL, etc.)
public typealias InAppBrowserCallbackType = @convention(c) (Int32) -> Void

// Platform availability of the in-app browser path:
//   - macOS (native/AppKit): SFSafariViewController is UIKit-only, so it is
//     unavailable here.
//   - visionOS: SFSafariViewController exists, but SFSafariViewControllerDelegate
//     does NOT — and we rely on safariViewControllerDidFinish(_:) to detect
//     dismissal, so visionOS is excluded.
//   - Mac Catalyst: both ARE available (since 13.1). It is left out here as an
//     iOS-first scope choice (not yet validated), not a hard limitation, and can
//     be enabled later since it shares the iOS code path.
// The plugin .meta also restricts compilation to iOS; this guard keeps the
// source self-documenting in case the meta drifts.
#if !targetEnvironment(macCatalyst) && !os(macOS) && !os(visionOS)
import UIKit
import SafariServices

private func iapLog(_ message: String) {
    NSLog("UnityIAP InAppBrowser: %@", message)
}

final class UnityPurchasingInAppBrowserController: NSObject, SFSafariViewControllerDelegate {

    static let shared = UnityPurchasingInAppBrowserController()

    private var currentViewController: SFSafariViewController?
    var callback: InAppBrowserCallbackType?

    private override init() {
        super.init()
    }

    func launch(urlString: String) {
        guard let url = URL(string: urlString) else {
            iapLog("Invalid URL: \(urlString)")
            DispatchQueue.main.async { [unowned self] in self.invokeCallback(reason: 1) }
            return
        }

        DispatchQueue.main.async { [unowned self] in
            guard let root = self.topMostViewController() else {
                iapLog("No root view controller available; cannot present in-app browser.")
                self.invokeCallback(reason: 1)
                return
            }

            // Defensive — managed side serializes purchases, but if a previous
            // browser is still presented we dismiss it before presenting a new one.
            if let previous = self.currentViewController {
                self.currentViewController = nil
                previous.dismiss(animated: false, completion: nil)
            }

            let safari = SFSafariViewController(url: url)
            safari.delegate = self
            if #available(iOS 11.0, *) {
                safari.dismissButtonStyle = .done
            }
            safari.modalPresentationStyle = .formSheet
            self.currentViewController = safari

            root.present(safari, animated: true, completion: nil)
        }
    }

    func dismiss() {
        DispatchQueue.main.async { [unowned self] in
            guard let vc = self.currentViewController else { return }
            self.currentViewController = nil
            vc.dismiss(animated: true) { [unowned self] in
                self.invokeCallback(reason: 0)
            }
        }
    }

    // MARK: - SFSafariViewControllerDelegate

    func safariViewControllerDidFinish(_ controller: SFSafariViewController) {
        if currentViewController === controller {
            currentViewController = nil
        }
        invokeCallback(reason: 0)
    }

    // MARK: - Helpers

    private func invokeCallback(reason: Int32) {
        guard let cb = callback else { return }
        if Thread.isMainThread {
            cb(reason)
        } else {
            DispatchQueue.main.async { cb(reason) }
        }
    }

    private func topMostViewController() -> UIViewController? {
        var root: UIViewController? = nil

        if #available(iOS 13.0, tvOS 13.0, *) {
            for scene in UIApplication.shared.connectedScenes {
                guard scene.activationState == .foregroundActive,
                      let windowScene = scene as? UIWindowScene else { continue }
                if let key = windowScene.windows.first(where: { $0.isKeyWindow }) {
                    root = key.rootViewController
                } else {
                    root = windowScene.windows.first?.rootViewController
                }
                if root != nil { break }
            }
        }

        if root == nil {
            // Fallback for older iOS / unusual scene configurations.
            // The .windows API is deprecated in iOS 15+ but still works as a safety net
            // when UIScene-based resolution returns nil.
            let allWindows: [UIWindow] = {
                // Use connectedScenes-flattened windows when possible (iOS 13+), .windows otherwise.
                if #available(iOS 13.0, tvOS 13.0, *)
                {
                    let sceneWindows = UIApplication.shared.connectedScenes
                        .compactMap { $0 as? UIWindowScene }
                        .flatMap { $0.windows }
                    if !sceneWindows.isEmpty { return sceneWindows }
                }
                return UIApplication.shared.windows
            }()
            root = allWindows.first(where: { $0.isKeyWindow })?.rootViewController
                ?? allWindows.first?.rootViewController
        }

        while let presented = root?.presentedViewController {
            root = presented
        }
        return root
    }
}

// MARK: - C entry points

@_cdecl("unityPurchasing_LaunchInAppBrowser")
public func unityPurchasing_LaunchInAppBrowser(_ urlCString: UnsafePointer<CChar>) {
    let urlString = String(cString: urlCString)
    UnityPurchasingInAppBrowserController.shared.launch(urlString: urlString)
}

@_cdecl("unityPurchasing_DismissInAppBrowser")
public func unityPurchasing_DismissInAppBrowser() {
    UnityPurchasingInAppBrowserController.shared.dismiss()
}

@_cdecl("unityPurchasing_SetInAppBrowserCallback")
public func unityPurchasing_SetInAppBrowserCallback(_ callback: @escaping InAppBrowserCallbackType) {
    UnityPurchasingInAppBrowserController.shared.callback = callback
}

#else

// macOS / Mac Catalyst: SFSafariViewController is iOS-only. Provide stub C
// entry points that report failure so the managed side falls back to OpenURL.

private var s_MacCallback: InAppBrowserCallbackType?

@_cdecl("unityPurchasing_LaunchInAppBrowser")
public func unityPurchasing_LaunchInAppBrowser(_ urlCString: UnsafePointer<CChar>) {
    NSLog("UnityIAP InAppBrowser: SFSafariViewController is not available on this platform; reporting failure.")
    if let cb = s_MacCallback {
        DispatchQueue.main.async { cb(1) }
    }
}

@_cdecl("unityPurchasing_DismissInAppBrowser")
public func unityPurchasing_DismissInAppBrowser() {
    // no-op
}

@_cdecl("unityPurchasing_SetInAppBrowserCallback")
public func unityPurchasing_SetInAppBrowserCallback(_ callback: @escaping InAppBrowserCallbackType) {
    s_MacCallback = callback
}

#endif
