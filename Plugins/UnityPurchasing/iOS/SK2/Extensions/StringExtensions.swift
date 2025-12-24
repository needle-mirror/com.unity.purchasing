import Foundation

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension String {

    func toCString() -> UnsafePointer<CChar> {
        let cString = self.withCString {
            $0
        }
        return cString
    }
}

