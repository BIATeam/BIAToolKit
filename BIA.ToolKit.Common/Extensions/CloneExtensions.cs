namespace BIA.ToolKit.Common.Extensions
{
    using Newtonsoft.Json;

    public static class CloneExtensions
    {
        /// <summary>
        /// Creates a deep clone of the specified object by serializing and deserializing it.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="self">The object to be cloned.</param>
        /// <returns>
        /// A new instance of an object of type T, having the identical values as the original object.
        /// If an error occurs during the serialization or deserialization process, the method will return null.
        /// The returned object is completely disassociated from the original object i.e., there is no sharing of references between both objects.
        /// </returns>
        /// <exception cref="JsonSerializationException">
        /// Thrown if the serialization or deserialization process encounters an error.
        /// </exception>
        public static T? DeepCopy<T>(this T self)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
            string serialized = JsonConvert.SerializeObject(self, jsonSerializerSettings);
            return JsonConvert.DeserializeObject<T>(serialized, jsonSerializerSettings);
        }
    }
}
