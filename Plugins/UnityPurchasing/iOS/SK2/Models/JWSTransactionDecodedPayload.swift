// Copyright (c) 2023 Apple Inc. Licensed under MIT License.
// Modified on September 5th, 2025; Removed all fields, except price.

import Foundation
///A decoded payload containing transaction information.
///
///[JWSTransactionDecodedPayload](https://developer.apple.com/documentation/appstoreserverapi/jwstransactiondecodedpayload)
public struct JWSTransactionDecodedPayload: Decodable, Hashable, Sendable {

    public init(price: Int64? = nil) {
        self.price = price
    }

    public var price: Int64?

    public enum CodingKeys: CodingKey {
        case price
    }

    public init(from decoder: any Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        self.price = try container.decodeIfPresent(Int64.self, forKey: .price)
    }
}
