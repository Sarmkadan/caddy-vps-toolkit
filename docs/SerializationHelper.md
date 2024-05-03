# SerializationHelper
The `SerializationHelper` class provides a set of static methods for serializing and deserializing objects to and from various formats, including JSON, XML, and dictionaries. It also offers methods for deep cloning objects and converting them to dictionaries. This class is designed to simplify the process of working with different data formats and to provide a convenient way to perform common serialization and deserialization tasks.

## API
* `public static string ToJson<T>(T obj)`: Converts an object of type `T` to a JSON string. The object is serialized using the default JSON serialization settings. Returns the JSON string representation of the object. Throws an exception if the object cannot be serialized to JSON.
* `public static T FromJson<T>(string json)`: Deserializes a JSON string to an object of type `T`. The JSON string is deserialized using the default JSON deserialization settings. Returns the deserialized object. Throws an exception if the JSON string cannot be deserialized to an object of type `T`.
* `public static T TryFromJson<T>(string json)`: Attempts to deserialize a JSON string to an object of type `T`. The JSON string is deserialized using the default JSON deserialization settings. Returns the deserialized object, or default(`T`) if the JSON string cannot be deserialized.
* `public static string ToXml<T>(T obj)`: Converts an object of type `T` to an XML string. The object is serialized using the default XML serialization settings. Returns the XML string representation of the object. Throws an exception if the object cannot be serialized to XML.
* `public static T FromXml<T>(string xml)`: Deserializes an XML string to an object of type `T`. The XML string is deserialized using the default XML deserialization settings. Returns the deserialized object. Throws an exception if the XML string cannot be deserialized to an object of type `T`.
* `public static Dictionary<string, object> ToDictionary<T>(T obj)`: Converts an object of type `T` to a dictionary. The object's properties are serialized to a dictionary using the default serialization settings. Returns the dictionary representation of the object.
* `public static T DeepClone<T>(T obj)`: Creates a deep clone of an object of type `T`. The object is cloned using the default cloning settings. Returns the cloned object.

## Usage
The following examples demonstrate how to use the `SerializationHelper` class:
```csharp
// Serialize an object to JSON
var person = new Person { Name = "John", Age = 30 };
var json = SerializationHelper.ToJson(person);
Console.WriteLine(json); // Output: {"Name":"John","Age":30}

// Deserialize a JSON string to an object
var json = "{\"Name\":\"Jane\",\"Age\":25}";
var person = SerializationHelper.FromJson<Person>(json);
Console.WriteLine(person.Name); // Output: Jane
Console.WriteLine(person.Age); // Output: 25
```

## Notes
When using the `SerializationHelper` class, note that the serialization and deserialization processes may throw exceptions if the objects being serialized or deserialized do not conform to the expected formats. Additionally, the `DeepClone` method may not work correctly for objects that contain references to other objects, as it uses a simple recursive cloning approach. The `SerializationHelper` class is not thread-safe, as it uses static methods and does not provide any synchronization mechanisms. Therefore, it is recommended to use this class in a single-threaded environment or to synchronize access to its methods using external synchronization mechanisms.
