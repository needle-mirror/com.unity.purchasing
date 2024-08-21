import Foundation

@available(iOS 13.0, *)
func encodeToData<T: Encodable>(_ item: T) -> Data? {
    do {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .withoutEscapingSlashes]
        let encodedJSON = try encoder.encode(item)
        return encodedJSON
    } catch {
        return nil
    }
}

@available(iOS 13.0, *)
func encodeToJSON<T: Encodable>(_ item: T) -> String {
    let encoder = JSONEncoder()
    encoder.outputFormatting = [.prettyPrinted, .withoutEscapingSlashes]
    if let jsonData = try? encoder.encode(item),
       let jsonString = String(data: jsonData, encoding: .utf8) {
        return jsonString
    }
    return ""
}

/**
 Decode json string to object type
 */
func decodeJSONToType<T: Decodable>(_ json: String,_ type: T.Type) throws -> T where T : Decodable {
    return try JSONDecoder().decode(type, from: json.data(using: .utf8)!)
}

func decodeDataToJSON<T: Decodable>(_: T.Type, data: Data) -> T? {
    do {
        let decodedJSON = try JSONDecoder().decode(T.self, from: data)
        return decodedJSON
    } catch {
        return nil
    }
}

/**
 Convert JSON CChar to a dictionary
 */
func dictionaryFromJSONCstr(_ json: UnsafePointer<CChar>) -> [String: AnyObject]? {
    let jsonString = String(cString: json)
    guard let data = jsonString.data(using: .utf8) else { return nil }
    let dict = try? JSONSerialization.jsonObject(with: data, options: []) as? [String: AnyObject]
    return dict
}
