@propertyWrapper
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct Dependency<T> {
    var wrappedValue: T

    init() {
        wrappedValue = DependencyContainer.resolve()
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
class DependencyContainer {
    private var dependencies = [String: AnyObject]()
    private static var shared = DependencyContainer()

    static func register<T>(_ dependency: T) {
        shared.register(dependency)
    }

    static func resolve<T>() -> T {
        shared.resolve()
    }

    private func register<T>(_ dependency: T) {
        let key = String(describing: T.self)
        dependencies[key] = dependency as AnyObject
    }

    private func resolve<T>() -> T {
        let key = String(describing: T.self)
        let dependency = dependencies[key] as? T

        precondition(dependency != nil, "No dependency found for \(key)! must register a dependency before resolve.")

        return dependency!
    }
}
