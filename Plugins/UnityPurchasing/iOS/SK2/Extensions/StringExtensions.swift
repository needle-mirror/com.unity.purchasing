import Foundation

extension String {

    func toCString() -> UnsafePointer<CChar> {
        let cString = self.withCString {
            $0
        }
        return cString
    }
}

