using System;

namespace PowerNetwork.Core.Helpers {
    public static class StringHelper {
        public static string ToTitleCase(this string str) {
            var tokens = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++) {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1).ToLower();
            }

            return string.Join(" ", tokens);
        }

        private static readonly string[] SpanishMonths =
            { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

        public static string ToSpanishMonth(this int month) {
            if (month < 1 || month > 12) return "";
            return SpanishMonths[month - 1];
        }
    }
}
