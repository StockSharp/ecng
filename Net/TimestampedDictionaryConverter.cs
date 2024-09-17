namespace Ecng.Net;

public class TimestampedDictionaryConverter : JsonConverter
{
	public override bool CanConvert(Type objectType) => true;

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		var result = Activator.CreateInstance(objectType);

		var types = objectType.GetGenericArguments();

		var typeKey = types[0];
		var typeValue = types[1];

		var propLast = objectType.GetProperty(nameof(TimestampedDictionary<int, int>.Last));
		var methodAdd = objectType.GetMethod(nameof(TimestampedDictionary<int, int>.Add), [typeKey, typeValue]);

		reader.Read();
		while (reader.TokenType != JsonToken.EndObject)
		{
			var key = reader.Value.ToString();
			reader.Read();

			if (key == "last")
			{
				propLast.SetValue(result, serializer.Deserialize<long>(reader), null);
			}
			else
			{
				methodAdd.Invoke(result, [key, serializer.Deserialize(reader, typeValue)]);
			}
			
			reader.Read();
		}

		return result;
	}

	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		=> throw new NotSupportedException();
}