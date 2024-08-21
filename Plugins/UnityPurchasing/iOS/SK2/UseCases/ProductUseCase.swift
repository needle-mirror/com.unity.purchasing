import StoreKit


/**
 This protocol can be implemented by any classes that can be used to retrieve StoreKit products.
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public protocol ProductUseCaseProtocol {
    func fetchProduct(for productId: String) async -> Product?
    func fetchProducts(for productIds: [String]) async -> ProductResponse
    func fetchSubscribtion(for productId: String) async -> SubscriptionInfoStatusResponse
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class ProductUseCase: ProductUseCaseProtocol {
    public func fetchProduct(for productId: String) async -> Product? {
        do {
            let products = try await Product.products(for: [productId])
            if products.count == 0 {
                return nil
            }
            return products[0]
        } catch {
            return nil
        }
    }
    
    public func fetchProducts(for productIds: [String]) async -> ProductResponse {
        var response: ProductResponse
        do {
            let products = try await Product.products(for: productIds)
            response = ProductResponse(products: products)
        } catch let error as StoreKitError {
            response = ProductResponse(error: error)
        } catch let error {
            response = ProductResponse(error: error)
        }

        return response
    }

    public func fetchSubscribtion(for productId: String) async -> SubscriptionInfoStatusResponse {
        do {
            let result = try await fetchSubscriptionStatus(for: productId)

            return SubscriptionInfoStatusResponse(productId: productId, statuses: result)
        } catch {
            return SubscriptionInfoStatusResponse(productId: productId, error: error)
        }
    }

    func fetchSubscriptionStatus(for productId: String) async throws -> [Product.SubscriptionInfo.Status] {
        let response = await fetchProducts(for: [productId])

        let products = response.products
        guard products.count > 0 else {
            throw StoreKitError.notAvailableInStorefront
        }

        guard products[0].subscription != nil else {
            throw SKResponseError(type: .customError, description: "Cannot fetch SubscriptionInfo.Status: unsupported Product type.", value: "unsupportedProductType")
        }

        let product = products[0]
        return try await product.subscription!.status
    }

    func getIntroductoryOfferType(product: Product) -> Product.SubscriptionOffer.PaymentMode? {
            if let introductoryOffer = product.subscription?.introductoryOffer {

                let introductoryOfferPaymentMode = introductoryOffer.paymentMode

                switch introductoryOfferPaymentMode {
                case .payAsYouGo:
                    printLog("getIntroductoryOfferType is pay as you go.")
                    return .payAsYouGo
                case .payUpFront:
                    printLog("getIntroductoryOfferType is pay up front.")
                    return .payUpFront
                case .freeTrial:
                    printLog("getIntroductoryOfferType is a free trial.")
                    return .freeTrial
                default:
                    fatalError("ERROR: YOU HAVE NOT CONSIDERED ALL INTRODUCTORY PAYMENT MODE TYPES.")
                }
            } else {
                printLog("getIntroductoryOfferType there is no introductory offer.")
                return nil
            }
    }
}
