using System.Reflection;

namespace SEAL_Domain.Ultis
{
    public static class EnumHelper
    {
        public class EnumValue
        {
            public int Value { get; set; }
            public string? Name { get; set; }
        }

        public static string Name<T>(this T srcValue) => GetCustomName(typeof(T).GetField(srcValue?.ToString() ?? string.Empty));
        private static string GetCustomName(FieldInfo? fi)
        {
            Type type = typeof(CustomName);

            Attribute? attr = null;
            if (fi is not null)
            {
                attr = Attribute.GetCustomAttribute(fi, type);
            }

            return (attr as CustomName)?.Name ?? string.Empty;
        }

        public static string GetName<T>(this T value)
        {
            FieldInfo? field = value?.GetType().GetField(value.ToString()!);
            Type type = typeof(CustomName);
            Attribute? attr = null;
            if (field is not null)
            {
                attr = Attribute.GetCustomAttribute(field, type);
            }
            return (attr as CustomName)?.Name ?? string.Empty;
        }

        public static IEnumerable<EnumValue> GetEnumValues<T>() where T : struct, Enum
        {
            var enums = Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new EnumValue
                {
                    Value = Convert.ToInt32(e),
                    Name = e.Name()
                });
            return enums;
        }
    }
}
