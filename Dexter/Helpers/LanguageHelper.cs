using Dexter.Configurations;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dexter.Helpers
{

	/// <summary>
	/// Holds a variety of tools to deal with organic string management/manipulation.
	/// </summary>

	public static class LanguageHelper
	{

		private enum ArticleType
		{
			None,
			Uppercase,
			Lowercase
		}

		private enum PluralType
		{
			None,
			Plural
		}

		private enum PossessiveType
		{
			None,
			Possessive
		}

		/// <summary>
		/// Randomizes special groups of characters in <paramref name="predicate"/> and fills them in with corresponding terms in <paramref name="termBanks"/>.
		/// </summary>
		/// <remarks><para>The way to mark an expression for randomization is to wrap it in braces ("{}"). The format of each expression must be as follows: {IDENTIFIERX}, where IDENTIFIER is a key in <paramref name="termBanks"/> and X is a positive integer value, every expression with the same identifier and value will be swapped for the same term.</para>
		/// <para>Special Identifiers exist, which do not run through terms: 'a' will try to guess the most probable expression of the indefinite article.</para></remarks>
		/// <param name="predicate">The message whose terms are to be randomized.</param>
		/// <param name="termBanks">A string-string[] dictionary where the keys are the explicit identifier of each TermClass and the values are list of terms those expressions can be substituted with.</param>
		/// <param name="random">A Random Number Generator used to extract random terms from <paramref name="termBanks"/>.</param>
		/// <param name="config">The LanguageConfiguration instance to run this process with.</param>
		/// <returns>A <c>string</c> holding the new, randomized predicate.</returns>

		public static string RandomizePredicate(string predicate, Dictionary<string, string[]> termBanks, Random random, LanguageConfiguration config)
		{
			HashSet<TermClass> terms = new();

			foreach (KeyValuePair<string, string[]> k in termBanks)
			{
				terms.Add(new TermClass(k.Key, k.Value));
			}

			StringBuilder newPredicate = new(predicate.Length * 2);

			ArticleType resolveArticle = ArticleType.None;
			PluralType resolvePlural = PluralType.None;
			PossessiveType resolvePossessive = PossessiveType.None;

			while (predicate.Length > 0)
			{
				int insertIndex = predicate.IndexOf(config.TermInsertionStartIndicator);
				if (insertIndex == -1)
				{
					newPredicate.Append(predicate);
					break;
				}

				int endIndex = predicate.IndexOf(config.TermInsertionEndIndicator);
				if (endIndex == -1)
				{
					throw new FormatException($"There was an error parsing predicate {predicate}, unbalanced braces. Please contact the developer team.");
				}

				newPredicate.Append(predicate[..insertIndex]);

				string compareString = predicate[(insertIndex + 1)..endIndex];

				//SPECIAL CASES
				if (compareString is "a" or "A")
				{
					resolveArticle = compareString == "a" ? ArticleType.Lowercase : ArticleType.Uppercase;
				}
				else if (compareString.ToLower() == "plural")
				{
					resolvePlural = PluralType.Plural;
				}
				else if (compareString.ToLower() == "\'s")
				{
					resolvePossessive = PossessiveType.Possessive;
				}
				else
				{
					//Default case
					foreach (TermClass term in terms)
					{
						int index = term.CheckReplace(compareString, config);

						if (index == (int)config.ErrorCodeInvalidArgument)
							throw new IndexOutOfRangeException($"There was an error parsing the number in term call \"{compareString}\" within \"{predicate}\". Please contact the developer team.");

						if (index >= 0)
						{
							string newTerm = term.GetOrGenerateCached(index, random, config);

							if (resolvePlural != PluralType.None)
							{
								newTerm = newTerm.GuessPlural(config);
							}

							if (resolvePossessive != PossessiveType.None)
							{
								newTerm = newTerm.Possessive();
							}

							if (resolveArticle != ArticleType.None)
							{
								newTerm = $"{GuessIndefiniteArticle(newTerm, resolveArticle == ArticleType.Uppercase)} {newTerm}";
							}

							newPredicate.Append(newTerm);

							resolvePlural = PluralType.None;
							resolveArticle = ArticleType.None;
							resolvePossessive = PossessiveType.None;
							break;
						}
					}
				}

				predicate = predicate[(endIndex + 1)..];
			}

			return newPredicate.ToString();
		}

		/// <summary>
		/// Checks whether an "IDENTIFIERX" expression shares an identifier with <paramref name="term"/>, and returns the index X.
		/// </summary>
		/// <param name="str">The raw form of the identifier-index expression, without braces.</param>
		/// <param name="term">The TermClass to compare <paramref name="str"/> against.</param>
		/// <param name="config">The LanguageConfiguration instance to run this process with.</param>
		/// <returns>The number accompanying the raw expression <paramref name="str"/> if their identifiers match, Invalid Number Error Code if the term X can't be parsed to a positive integer, -1 otherwise.</returns>

		private static int CheckReplace(this TermClass term, string str, LanguageConfiguration config)
		{
			if (!str.StartsWith(term.Identifier))
				return -1;

			if (int.TryParse(str[term.Identifier.Length..], out int index))
			{
				return index >= 0 ? index : (int)config.ErrorCodeInvalidArgument;
			}

			return (int)config.ErrorCodeInvalidArgument;
		}

		/// <summary>
		/// Holds a list of terms related by a common Identifier and provides a way to generate random terms.
		/// </summary>

		internal class TermClass
		{

			public readonly string Identifier;
			private readonly string[] Bank;
			private readonly List<string> Cache;

			public TermClass(string identifier, string[] bank)
			{
				Identifier = identifier;
				Bank = bank;
				Cache = new List<string>();
			}

			/// <summary>
			/// Gets a cached term located at <paramref name="index"/>, if Cache doesn't have those many elements, it generates elements up to <paramref name="index"/>.
			/// </summary>
			/// <remarks>The TermClass will attempt to generate new terms that aren't in its cache. To disable this, set <paramref name="maxRetries"/> to 0.</remarks>
			/// <param name="index">The index to get from the cache of terms stored in this TermClass.</param>
			/// <param name="random">A random number generator.</param>
			/// <param name="config">The LanguageConfiguration instance to run this process with.</param>
			/// <param name="maxRetries">The maximum amount of attempts to generate a novel term from the bank.</param>
			/// <returns>The term at position <paramref name="index"/> within the Cache.</returns>

			public string GetOrGenerateCached(int index, Random random, LanguageConfiguration config, int maxRetries = -1)
			{
				if (maxRetries < 0)
					maxRetries = (int)config.TermRepetitionAversionFactor;

				while (Cache.Count <= index)
				{
					Cache.Add(Bank[random.Next(Bank.Length)]);

					//If this term is already in the cache, try to replace it for a new, random one.
					for (int i = 0; i < maxRetries && Cache.IndexOf(Cache[^1]) < Cache.Count - 1; i++)
					{
						Cache[^1] = Bank[random.Next(Bank.Length)];
					}
				}

				return Cache[index];
			}
		}

		private static readonly Dictionary<long, string> BasicUnits = new()
		{
			{ 1000000000000, "T" },
			{ 1000000000, "B" },
			{ 1000000, "M" },
			{ 1000, "K" }
		};

		/// <summary>
		/// Represents a units dictionary for binary memory units.
		/// </summary>

		public static readonly Dictionary<long, string> ByteUnits = new()
		{
			{ 1099511627776, "TB" },
			{ 1073741824, "GB" },
			{ 1048576, "MB" },
			{ 1024, "KB" }
		};

		/// <summary>
		/// Represents a units dictionary for metric prefixes up to "tera"
		/// </summary>

		public static readonly Dictionary<long, string> MetricPrefixes = new()
		{
			{ 1000000000000, "T" },
			{ 1000000000, "G" },
			{ 1000000, "M" },
			{ 1000, "K" }
		};

		/// <summary>
		/// Converts an XP amount into a shortened version using suffixes.
		/// </summary>
		/// <param name="v">The value to simplify.</param>
		/// <param name="units">The units dictionary; if <see langword="null"/>, it will default to <see cref="BasicUnits"/>.</param>
		/// <returns>A string containing the shortened value.</returns>

		public static string ToUnit(this long v, Dictionary<long, string> units = null)
		{
			if (units is null) units = BasicUnits;
			foreach (KeyValuePair<long, string> kvp in units)
			{
				if (v >= kvp.Key)
				{
					return $"{(float)v / kvp.Key:G3}{kvp.Value}";
				}
			}
			return v.ToString();
		}

		/// <summary>
		/// Will attempt to guess whether the indefinite article should be 'a' or 'an' based on <paramref name="nextWord"/>.
		/// </summary>
		/// <param name="nextWord">A string describing what follows the article.</param>
		/// <param name="capitalize">Whether to capitalize the first letter of the article.</param>
		/// <returns>A string, either "a", "an", or "a(n)", where the character 'a' is capitalized if <paramref name="capitalize"/> is set to <see langword="true"/>.</returns>

		public static string GuessIndefiniteArticle(string nextWord, bool capitalize = false)
		{
			string relevant = nextWord.Trim().Split(' ')[0].ToLower();

			return (relevant[0]) switch
			{
				'a' or 'e' or 'i' or 'o' => capitalize ? "An" : "an",
				'h' or 'u' => capitalize ? "A(n)" : "a(n)",
				_ => capitalize ? "A" : "a",
			};
		}

		/// <summary>
		/// Guesses the most likely plural form of a noun from a set of English pluralization rules and irregular plurals.
		/// </summary>
		/// <param name="singular">The singular noun to pluralize.</param>
		/// <param name="config">The LanguageConfiguration instance to run this process with.</param>
		/// <returns>A string containing the pluralized form of <paramref name="singular"/>.</returns>

		public static string GuessPlural(this string singular, LanguageConfiguration config)
		{
			string lowerSingular = singular.ToLower();

			if (config.IrregularPlurals.ContainsKey(lowerSingular))
			{
				return config.IrregularPlurals[lowerSingular].MatchCase(singular);
			}
			else if (lowerSingular.EndsWith("on"))
			{
				return singular[..^2] + "a";
			}
			else if (lowerSingular.EndsWith("um"))
			{
				return singular[..^2] + "a";
			}
			else if (lowerSingular.EndsWith("us"))
			{
				return singular[..^2] + "i";
			}
			else if (lowerSingular.EndsWith("is"))
			{
				return singular[..^2] + "es";
			}
			else if (lowerSingular.EndsWith("ex") || lowerSingular.EndsWith("ix"))
			{
				return singular[..^2] + "ices";
			}
			else if (lowerSingular[^1] is 's' or 'z')
			{
				if (lowerSingular.Length > 2 && lowerSingular[^3].IsConsonant() && lowerSingular[^2].IsVowel() && lowerSingular[^1].IsConsonant())
				{
					return singular + singular[^1] + "es";
				}
				else
				{
					return singular + "es";
				}
			}
			else if (lowerSingular.EndsWith("fe"))
			{
				return singular[..^2] + "ves";
			}
			else if (lowerSingular.EndsWith("f"))
			{
				return singular[..^1] + "ves";
			}
			else if (lowerSingular.EndsWith("y"))
			{
				return singular[^2].IsVowel() ? singular + "s" : singular[^1] + "ies";
			}
			else if (lowerSingular[^1] is 'o' or 'x' || lowerSingular.EndsWith("sh") || lowerSingular.EndsWith("ch"))
			{
				return singular + "es";
			}

			return singular + "s";
		}

		/// <summary>
		/// Limits the given string <paramref name="s"/> to a length <paramref name="maxLength"/>.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="maxLength"></param>
		/// <returns>A substring of <paramref name="s"/> ending in "..." if its length is greater than <paramref name="maxLength"/>, otherwise <paramref name="s"/></returns>

		public static string TruncateTo(this string s, int maxLength)
		{
			if (s.Length > maxLength)
			{
				if (maxLength < 3) return "...";
				return s[..(maxLength - 3)] + "...";
			}

			return s;
		}

		/// <summary>
		/// A set of characters that discord uses for formatting.
		/// </summary>

		public static readonly HashSet<char> DiscordRichTextChars = new() { '*', '_', '`', '|' };

		/// <summary>
		/// Truncates a string to a given length similar to the <see cref="TruncateTo"/> method, but ignores characters marked in <paramref name="ignoreChars"/>.
		/// </summary>
		/// <param name="s">The original string to truncate.</param>
		/// <param name="maxLength">The maximum length to truncate <paramref name="s"/> to.</param>
		/// <param name="ignoreChars">The set of characters to ignore for counting.</param>
		/// <returns></returns>

		public static string TruncateToSpecial(this string s, int maxLength, HashSet<char> ignoreChars)
		{
			StringBuilder b = new(maxLength);
			int counter = 0;
			foreach (char c in s)
			{
				if (ignoreChars.Contains(c)) b.Append(c);
				else
				{
					if (++counter > maxLength - 3)
					{
						b.Append("...");
						break;
					}
					b.Append(c);
				}
			}
			return b.ToString();
		}

		/// <summary>
		/// Computes the length of a string ignoring all characters in <paramref name="ignoreChars"/>.
		/// </summary>
		/// <param name="s">The string whose length is to be computed.</param>
		/// <param name="ignoreChars">The set of characters which will not contribute towards length.</param>
		/// <returns>The length of the given string <paramref name="s"/> if all characters in <paramref name="ignoreChars"/> were removed from it.</returns>

		public static int LengthSpecial(this IReadOnlyCollection<char> s, HashSet<char> ignoreChars)
		{
			int result = 0;
			foreach (char c in s)
			{
				if (!ignoreChars.Contains(c)) result++;
			}
			return result;
		}

		/// <summary>
		/// Computes the length of a <see cref="StringBuilder"/> ignoring all characters in <paramref name="ignoreChars"/>.
		/// </summary>
		/// <param name="b">The <see cref="StringBuilder"/> whose length is to be computed.</param>
		/// <param name="ignoreChars">The set of characters which will not contribute towards length.</param>
		/// <returns>The length of the given string represented by <paramref name="b"/> if all characters in <paramref name="ignoreChars"/> were removed from it.</returns>

		public static int LengthSpecial(this StringBuilder b, HashSet<char> ignoreChars)
		{
			int result = 0;
			for (int i = 0; i < b.Length; i++)
			{
				if (!ignoreChars.Contains(b[i])) result++;
			}
			return result;
		}

		/// <summary>
		/// Checks whether a character is a vowel in the Latin alphabet
		/// </summary>
		/// <param name="c">The character to check</param>
		/// <returns><see langword="true"/> if <paramref name="c"/> is a vowel, <see langword="false"/> otherwise.</returns>

		public static bool IsVowel(this char c) { return char.ToLower(c) is 'a' or 'e' or 'i' or 'o' or 'u'; }

		/// <summary>
		/// Checks whether a character is a consonant in the Latin alphabet
		/// </summary>
		/// <param name="c">The character to check</param>
		/// <returns><see langword="true"/> if <paramref name="c"/> is a consonant, <see langword="false"/> otherwise.</returns>

		public static bool IsConsonant(this char c) { return char.ToLower(c) >= 'a' && char.ToLower(c) <= 'z' && !IsVowel(c); }

		/// <summary>
		/// Matches the case of <paramref name="input"/> to that of <paramref name="case"/>.
		/// </summary>
		/// <param name="input">The sequence of letters to convert to <paramref name="case"/>.</param>
		/// <param name="case">The uppercase-lowercase sequence to follow. If the length differs from <paramref name="input"/>, case-matching will stop..</param>
		/// <returns>A string with the same letters as <paramref name="input"/> and the same case as <paramref name="case"/>.</returns>

		public static string MatchCase(this string input, string @case)
		{
			int match = input.Length < @case.Length ? input.Length : @case.Length;

			StringBuilder stringBuilder = new(input.Length);

			for (int i = 0; i < match; i++)
			{
				stringBuilder.Append(input[i].MatchCase(@case[i]));
			}

			if (match < input.Length)
			{
				stringBuilder.Append(input[match..]);
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Matches the case of <paramref name="input"/> to that of <paramref name="case"/>.
		/// </summary>
		/// <param name="input">The character to convert <paramref name="case"/>.</param>
		/// <param name="case">The uppercase or lowercase character to reference for conversion of <paramref name="input"/>.</param>
		/// <returns>The character obtained from <paramref name="input"/> in the same case as <paramref name="case"/>.</returns>

		public static char MatchCase(this char input, char @case)
		{
			if (char.IsUpper(@case))
				return char.ToUpper(input);

			return char.ToLower(input);
		}

		/// <summary>
		/// Obtains the ordinal form of an integer <paramref name="num"/>.
		/// </summary>
		/// <param name="num">The base number to obtain the ordinal from.</param>
		/// <returns>The string "<paramref name="num"/>st", "<paramref name="num"/>nd", "<paramref name="num"/>rd" if any are appropriate, otherwise "<paramref name="num"/>th".</returns>

		public static string Ordinal(this int num)
		{
			if (num < 0)
				num = -num;

			if (num % 100 / 10 == 1)
				return num + "th";

			return (num % 10) switch
			{
				1 => num + "st",
				2 => num + "nd",
				3 => num + "rd",
				_ => num + "th"
			};
		}

		/// <summary>
		/// Adds the appropriate form of the possessive "'s" to a term <paramref name="input"/>.
		/// </summary>
		/// <param name="input">The noun to add the possessive termination to.</param>
		/// <returns>"<paramref name="input"/>'" if <paramref name="input"/> ends in 's'. "<paramref name="input"/>'s" otherwise.</returns>

		public static string Possessive(this string input)
		{
			return $"{input}'{(input.EndsWith('s') ? "" : "s")}";
		}

		/// <summary>
		/// Creates a human-readable expression of a hexagesimal-base system (such as hours and minutes).
		/// </summary>
		/// <param name="value">The decimal value corresponding to the <paramref name="largeUnit"/>.</param>
		/// <param name="largeUnit">The names of the larger unit, [0] is singular and [1] is plural.</param>
		/// <param name="smallUnit">The names of the smaller unit, [0] is singular and [1] is plural.</param>
		/// <param name="remainder">The decimal value that was disregarded after comparing the units.</param>
		/// <returns>A humanized expression of <paramref name="value"/> <paramref name="largeUnit"/>.</returns>

		public static string HumanizeSexagesimalUnits(float value, string[] largeUnit, string[] smallUnit, out float remainder)
		{
			return HumanizeOffbaseUnits(60, value, largeUnit, smallUnit, out remainder);
		}

		/// <summary>
		/// Creates a human-readable expression of a value in an arbitrary base with multiple Units. (Like feet and inches or degrees and arcminutes)
		/// </summary>
		/// <param name="baseN">The base of the counting system used to differentiate <paramref name="largeUnit"/> and <paramref name="smallUnit"/>.</param>
		/// <param name="value">The decimal value corresponding to the <paramref name="largeUnit"/>.</param>
		/// <param name="largeUnit">The names of the larger unit, [0] is singular and [1] is plural.</param>
		/// <param name="smallUnit">The names of the smaller unit, [0] is singular and [1] is plural.</param>
		/// <param name="remainder">The decimal value that was disregarded after comparing the units.</param>
		/// <returns>A humanized expression of <paramref name="value"/> <paramref name="largeUnit"/>.</returns>

		public static string HumanizeOffbaseUnits(int baseN, float value, string[] largeUnit, string[] smallUnit, out float remainder)
		{
			List<string> result = new();

			int largeValue = (int)value;
			int smallValue = (int)Math.Round(value % 1 * baseN);

			remainder = value - largeValue - (float)smallValue / baseN;

			if (largeValue != 0) result.Add($"{largeValue} {largeUnit[largeValue == 1 ? 0 : 1]}");
			if (smallValue != 0) result.Add($"{smallValue} {smallUnit[smallValue == 1 ? 0 : 1]}");

			if (result.Count == 0) return $"0 {largeUnit[1]}";

			return string.Join(" and ", result);
		}

		/// <summary>
		/// Enumerates a list of objects using the typical linguistic conventions for enumeration.
		/// </summary>
		/// <param name="inputList">The list of objects to enumerate.</param>
		/// <returns>A string with the enumeration expressed in a human-readable form.</returns>

		public static string Enumerate(this IEnumerable<object> inputList)
		{
			switch(inputList.Count())
			{
				case 0:
					return "";
				case 1:
					return inputList.First().ToString();
				case 2:
					return $"{inputList.First()} and {inputList.Last()}";
				default:
					string[] toEnumerate = new string[inputList.Count()];
					int i = 0;
					foreach(object o in inputList)
					{
						toEnumerate[i++] = o.ToString();
					}
					return $"{string.Join(", ", toEnumerate[..^1])} and {toEnumerate[^1]}";
			}
		}

		/// <summary>
		/// Extracts substrings that fit a given url schema from an <paramref name="input"/> string.
		/// </summary>
		/// <remarks>All potential links in the string must be encapsulated in parentheses or spaces.</remarks>
		/// <param name="input">The string to analyze and extracts urls from.</param>
		/// <returns>A <c>string[]</c> array containing a collection of substrings that matched the url pattern in <paramref name="input"/>.</returns>

		public static string[] GetHyperLinks(this string input)
		{
			List<string> matches = new();

			input = ' ' + input + ' ';

			List<int> openers = new();
			List<int> closers = new();

			for (int i = 0; i < input.Length; i++)
			{
				switch (input[i])
				{
					case ' ':
						closers.Add(i);
						matches.AddRange(input.CheckForLinks(openers, closers));

						openers.Clear();
						closers.Clear();
						openers.Add(i);
						break;
					case ')':
						closers.Add(i);
						break;
					case '(':
						openers.Add(i);
						break;
				}
			}

			return matches.ToArray();
		}

		private static string[] CheckForLinks(this string input, IEnumerable<int> openers, IEnumerable<int> closers)
		{
			List<string> Result = new();

			foreach (int o in openers)
			{
				foreach (int c in closers)
				{
					if (c > o)
					{
						string s = input[(o + 1)..c];
						if (s.IsHyperLink())
						{
							Result.Add(s);
						}
					}
				}
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Checks whether a given <paramref name="input"/> string is a url.
		/// </summary>
		/// <param name="input">The string to check.</param>
		/// <returns><see langword="true"/> if the given string is a url; otherwise <see langword="false"/>.</returns>

		public static bool IsHyperLink(this string input)
		{
			return Regex.IsMatch(input, @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=]*)?$");
		}

		/// <summary>
		/// Indicates the ways to express a date that will be parsed successfully.
		/// </summary>

		public const string DEFAULT_DATE_FORMAT_INFO = "`Month dd((,) year)`  __*OR*__  `dd Month((,) year)`  __*OR*__  `(MM/dd(/year)`";

		/// <summary>
		/// Indicates the ways to express a time that will be parsed successfully.
		/// </summary>

		public const string DEFAULT_TIME_FORMAT_INFO = "`hh:mm(:ss(.ffff)) (<AM/PM>)`  __*OR*__  `hh <AM/PM>`";

		/// <summary>
		/// Indicates the ways to express a TimeSpan Offset for a time zone that will be parsed successfully.
		/// </summary>

		public const string DEFAULT_OFFSET_FORMAT_INFO = "`TZA`  __*OR*__  `(<UTC/GMT/Z>)<+/->h(:mm)`";


		/// <summary>
		/// Attempts to parse a string expressing a date, time, and offset. All elements except for the hour and minute are optional.
		/// </summary>
		/// <param name="input">The stringified expression of the date to be parsed into <paramref name="time"/>.</param>
		/// <param name="culture">The Cultural Context with which to parse the date given in <paramref name="input"/>.</param>
		/// <param name="config">The Configuration related to parsing linguistic humanized information like time zone abbreviations.</param>
		/// <param name="time">The parsed <c>DateTimeOffset</c> extracted from <paramref name="input"/>.</param>
		/// <param name="error">The reason the parsing failed if it did.</param>
		/// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

		public static bool TryParseTime(this string input, CultureInfo culture, LanguageConfiguration config, out DateTimeOffset time, out string error)
		{

			error = "";
			input = input.Trim();
			time = DateTimeOffset.Now;

			string lowerInput = input.ToLower();

			switch (lowerInput)
			{
				case "now":
					time = DateTimeOffset.Now;
					return true;
			}

			string timeZoneMatcher = @"(((UTC|GMT|Z)?[+-][0-9]{1,2}(:[0-9]{2})?)|([A-Z][A-Za-z0-9]*))$";
			string timeZoneSegment = Regex.Match(input, timeZoneMatcher).Value;

			TimeZoneData timeZone = null;
			TimeSpan timeZoneOffset = DateTimeOffset.Now.Offset;

			if (!string.IsNullOrEmpty(timeZoneSegment))
			{
				if (timeZoneSegment.Contains('+') || timeZoneSegment.Contains('-') || config.TimeZones.ContainsKey(timeZoneSegment))
				{
					if (TimeZoneData.TryParse(timeZoneSegment, config, out timeZone))
					{
						timeZoneOffset = timeZone.TimeOffset;
					}
				}
			}

			if (Regex.IsMatch(lowerInput, @$"(^in)|(from now\s*{timeZoneMatcher}?[\s.]*$)", RegexOptions.IgnoreCase))
			{
				if (!TryParseSpan(input, out TimeSpan span, out string newError))
				{
					error = newError;
					return false;
				}
				time = time.Add(span).Add(TimeSpan.FromMilliseconds(100)).ToOffset(timeZoneOffset);
				return true;
			}
			else if (Regex.IsMatch(lowerInput, @$"ago\s*{timeZoneMatcher}?[\s.]*$", RegexOptions.IgnoreCase))
			{
				if (!TryParseSpan(input, out TimeSpan span, out string newError))
				{
					error = newError;
					return false;
				}
				time = time.Subtract(span).Subtract(TimeSpan.FromMilliseconds(100)).ToOffset(timeZoneOffset);
				return true;
			}

			foreach (Match m in Regex.Matches(input, @"[0-9](st|nd|rd|th)"))
			{
				input = $"{input[..m.Index]}{m.Value[0]}{(input.Length > m.Index + m.Length ? input[(m.Index + m.Length)..] : "")}";
			}

			string dateStrSegment = Regex.Match(input, @"(^|\s)(([A-Za-z]{3,}\s[0-9]{1,2})|([0-9]{1,2}\s[A-Za-z]{3,}))((,|\s)\s?[0-9]{2,5}(\s|$))?").Value.Trim();
			string dateNumSegment = Regex.Match(input, @"[0-9]{1,2}\/[0-9]{1,2}(\/[0-9]{2,5})?").Value;
			string timeSimplifiedSegment = Regex.Match(input, @"(^|\s)[0-9]{1,2}\s?[pa]m(\s|$)", RegexOptions.IgnoreCase).Value;
			string timeSegment = Regex.Match(input, @"[0-9]{1,2}:[0-9]{1,2}(:[0-9]{1,2}(.[0-9]+)?)?(\s?(a|p)m)?", RegexOptions.IgnoreCase).Value;

			DateTimeOffset offsetNow = DateTimeOffset.Now.ToOffset(timeZoneOffset);

			int year = offsetNow.Year;
			int month = offsetNow.Month;
			int day = offsetNow.Day;
			int hour = offsetNow.Hour;
			int minute = offsetNow.Minute;
			float second = 0;

			if (!string.IsNullOrEmpty(dateStrSegment))
			{
				dateStrSegment = dateStrSegment.Replace(", ", " ").Replace(",", " ");

				string[] MDY = dateStrSegment.Split(" ");
				string dd;

				month = ParseMonth(MDY[0]);
				if (month < 0)
				{
					month = ParseMonth(MDY[1]);
					if (month < 0)
					{
						error = $"Failed to parse \"{MDY[0]}\" OR \"{MDY[1]}\" into a valid Month.";
						return false;
					}
					dd = MDY[0];
				}
				else
				{
					dd = MDY[1];
				}

				if (!int.TryParse(dd, out day))
				{
					error = $"Failed to parse {dd} into a valid Day of the Month.";
					return false;
				}

				if (day < 0 || day > 31) { return false; }

				if (MDY.Length > 2)
				{
					if (!int.TryParse(MDY[2], out year))
					{
						error = $"Failed to parse {MDY[2]} into a valid year!";
						return false;
					}
					if (year < 100) year += 2000; //YY parsing
					if (year > 10000) year -= 10000; //Human Era Parsing
					if (year < 100 || year > 3000)
					{
						error = $"Year {year} is outside the range of valid accepted years (must be between 100 and 3000)";
						return false;
					}
				}
			}
			else if (!string.IsNullOrEmpty(dateNumSegment))
			{
				if (dateNumSegment.Split("/").Length < 3)
				{
					dateNumSegment += $"/{year}";
				}

				DateTime Subparse;
				try
				{
					Subparse = DateTime.Parse(dateNumSegment, culture);
				}
				catch (FormatException e)
				{
					error = e.Message;
					return false;
				}

				day = Subparse.Day;
				month = Subparse.Month;
				year = Subparse.Year;
			}

			TimeMeridianDiscriminator TMD = TimeMeridianDiscriminator.H24;

			if (!string.IsNullOrEmpty(timeSimplifiedSegment))
			{
				TMD = timeSimplifiedSegment.Trim()[^2] is 'p' or 'P' ? TimeMeridianDiscriminator.PM : TimeMeridianDiscriminator.AM;
				hour = int.Parse(timeSimplifiedSegment.Trim()[..^2]);
				minute = 0;
			}
			else
			{
				if (string.IsNullOrEmpty(timeSegment))
				{
					if (string.IsNullOrEmpty(dateNumSegment) && string.IsNullOrEmpty(dateStrSegment))
					{
						error = "A time or day must be provided! Time segments are formatted as: `hh:mm(:ss) (<am/pm>)`";
						return false;
					}
					else
					{
						TMD = TimeMeridianDiscriminator.H24;
						hour = 0;
						minute = 0;
						second = 0;
					}
				}
				else
				{
					if (timeSegment[^1] is 'm' or 'M')
					{
						TMD = timeSegment[^2] is 'p' or 'P' ? TimeMeridianDiscriminator.PM : TimeMeridianDiscriminator.AM;
						timeSegment = timeSegment[..^2];
					}

					string[] hmsf = timeSegment.Trim().Split(":");
					hour = int.Parse(hmsf[0]);
					minute = int.Parse(hmsf[1]);

					if (hmsf.Length > 2) second = float.Parse(hmsf[2]);
				}
			}

			if (TMD == TimeMeridianDiscriminator.AM && hour == 12) hour = 0;
			else if (TMD == TimeMeridianDiscriminator.PM && hour != 12) hour += 12;

			try
			{
				time = new DateTimeOffset(new DateTime(year, month, day, hour, minute, (int)second, (int)(second % 1 * 1000)), timeZoneOffset);
			}
			catch (ArgumentOutOfRangeException e)
			{
				error = $"Impossible to parse to a valid time! Are you sure the month you chose has enough days?\n" +
					$"Selected numbers are Year: {year}, Month: {month}, Day: {day}, Hour: {hour}, Minute: {minute}, Second: {second}, Time Zone: {timeZone?.ToString() ?? TimeZoneData.ToTimeZoneExpression(timeZoneOffset)}.\n[{e.Message}]";
				return false;
			}

			return true;
		}

		private enum TimeMeridianDiscriminator
		{
			H24,
			AM,
			PM
		}

		/// <summary>
		/// Attempts to parse a Month given a CultureInfo for Month Names and Abbreviated Month Names.
		/// </summary>
		/// <param name="input">An abbreviated or complete month name in accordance to <paramref name="cultureInfo"/>, case-insensitive.</param>
		/// <param name="cultureInfo">The contextual CultureInfo containing the calendar information and month names.</param>
		/// <returns><c>-1</c> if the parsing is unsuccessful, otherwise the number corresponding to the month (1 for January, 2 for February... etc.)</returns>

		public static int ParseMonth(this string input, CultureInfo cultureInfo = null)
		{
			if (cultureInfo == null) cultureInfo = CultureInfo.InvariantCulture;

			input = input.ToLower();

			for (int i = 0; i < cultureInfo.DateTimeFormat.MonthNames.Length; i++)
			{
				if (input == cultureInfo.DateTimeFormat.MonthNames[i].ToLower() || input == cultureInfo.DateTimeFormat.AbbreviatedMonthNames[i].ToLower())
				{
					return i + 1;
				}
			}

			return -1;
		}

		/// <summary>
		/// Attempts to parse a Month given a CultureInfo for Month Names and Abbreviated Month Names.
		/// </summary>
		/// <param name="input">An abbreviated or complete month name in accordance to <paramref name="cultureInfo"/>, case-insensitive.</param>
		/// <param name="cultureInfo">The contextual CultureInfo containing the calendar information and month names.</param>
		/// <returns>The Month enum corresponding to the parsed month, or <see cref="Month.None"/> if none match.</returns>

		public static Month ParseMonthEnum(this string input, CultureInfo cultureInfo = null)
		{
			int m = ParseMonth(input, cultureInfo);

			if (m < 0) return Month.None;
			else return (Month)m;
		}

		/// <summary>
		/// Attempts to obtain a <see cref="Weekday"/> from a string representation of it.
		/// </summary>
		/// <param name="input">The raw stringified expression.</param>
		/// <param name="weekday">The result of the operation if successful.</param>
		/// <param name="feedback">The description of the result or error in the operation.</param>
		/// <returns><see langword="true"/> if the parsing is successful, otherwise <see langword="false"/>.</returns>

		public static bool TryParseWeekday(this string input, out Weekday weekday, out string feedback)
		{

			HashSet<string> days = CultureInfo.InvariantCulture.DateTimeFormat.DayNames.ToHashSet();
			input = input.ToLower();
			weekday = Weekday.None;

			for (int i = 0; i < input.Length; i++)
			{
				days.RemoveWhere((d) => d.Length < i || char.ToLower(d[i]) != input[i]);
			}

			if (days.Count == 0)
			{
				feedback = $"No days of the week start with the sequence {input}.";
				return false;
			}
			else if (days.Count > 1)
			{
				feedback = $"Input is ambiguouos between the following possible terms: {string.Join(", ", days)}.";
				return false;
			}

			if (!Enum.TryParse(days.First(), true, out weekday))
			{
				feedback = $"Unable to parse {days.First()} to a valid standard weekday, if you're using the English language, this is an error; please contact a developer so it can be fixed.";
				return false;
			}
			feedback = $"Parsed {weekday} from {input}.";
			return true;
		}

		/// <summary>
		/// Represents a day of the week.
		/// </summary>

		public enum Weekday : byte
		{
			/// <summary>
			/// Represents the 1st day of the work week.
			/// </summary>		
			Monday,
			/// <summary>
			/// Represents the 2nd day of the work week.
			/// </summary>  
			Tuesday,
			/// <summary>
			/// Represents the 3rd day of the work week.
			/// </summary>  
			Wednesday,
			/// <summary>
			/// Represents the 4th day of the work week.
			/// </summary>  
			Thursday,
			/// <summary>
			/// Represents the 5th day of the work week.
			/// </summary>  
			Friday,
			/// <summary>
			/// Represents the 1st day of the weekend.
			/// </summary> 
			Saturday,
			/// <summary>
			/// Represents the 2nd day of the weekend.
			/// </summary>  
			Sunday,
			/// <summary>
			/// Represents a non-valid weekday.
			/// </summary>
			None
		}

		/// <summary>
		/// Represents a month in the year
		/// </summary>

		public enum Month : byte
		{
			/// <summary>
			/// Represents an invalid month of the year.
			/// </summary>
			None,
			/// <summary>
			/// Represents the 1st month of the year.
			/// </summary>
			January,
			/// <summary>
			/// Represents the 2nd month of the year.
			/// </summary>
			February,
			/// <summary>
			/// Represents the 3rd month of the year.
			/// </summary>
			March,
			/// <summary>
			/// Represents the 4th month of the year.
			/// </summary>
			April,
			/// <summary>
			/// Represents the 5th month of the year.
			/// </summary>
			May,
			/// <summary>
			/// Represents the 6th month of the year.
			/// </summary>
			June,
			/// <summary>
			/// Represents the 7th month of the year.
			/// </summary>
			July,
			/// <summary>
			/// Represents the 8th month of the year.
			/// </summary>
			August,
			/// <summary>
			/// Represents the 9th month of the year.
			/// </summary>
			September,
			/// <summary>
			/// Represents the 10th month of the year.
			/// </summary>
			October,
			/// <summary>
			/// Represents the 11th month of the year.
			/// </summary>
			November,
			/// <summary>
			/// Represents the 12th month of the year.
			/// </summary>
			December
		}

		/// <summary>
		/// Attempts to find a time zone by the abbreviated name of <paramref name="input"/> and returns it as a TimeSpan to be used as an Offset. 
		/// </summary>
		/// <param name="input">The abbreviation of the time zone as it appears in <paramref name="languageConfiguration"/>.</param>
		/// <param name="languageConfiguration">The Config file containing data on Time Zone names and their respective offsets.</param>
		/// <param name="timeZone">The output value of the parsed <paramref name="input"/>, or <see langword="null"/> if it can't be parsed.</param>
		/// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

		public static bool TryParseTimeZone(this string input, LanguageConfiguration languageConfiguration, out TimeZoneData timeZone)
		{
			if (languageConfiguration.TimeZones.ContainsKey(input))
			{
				timeZone = languageConfiguration.TimeZones[input];
				return true;
			}
			else if (languageConfiguration.TimeZones.ContainsKey(input.ToUpper()))
			{
				timeZone = languageConfiguration.TimeZones[input.ToUpper()];
				return true;
			}

			timeZone = null;
			return false;
		}

		/// <summary>
		/// Attempts to parse a timespan out of humanized terms
		/// </summary>
		/// <param name="input">A stringified time expression consisting of a series of numbers followed by units.</param>
		/// <param name="span">The output span resulting from the parsing of <paramref name="input"/>.</param>
		/// <param name="error">Empty string if there are no errors, otherwise describes why it failed.</param>
		/// <returns><see langword="true"/> if the parsing was successful, otherwise <see langword="false"/>.</returns>

		public static bool TryParseSpan(this string input, out TimeSpan span, out string error)
		{
			error = "";
			span = TimeSpan.Zero;

			Dictionary<TimeUnit, string> regExps = new()
			{
				{ TimeUnit.Millisecond, @"(ms)|((milli)(second)?s?)" },
				{ TimeUnit.Second, @"s(ec(ond)?s?)?" },
				{ TimeUnit.Minute, @"m(in(ute)?s?)?" },
				{ TimeUnit.Hour, @"h((ou)?rs?)?" },
				{ TimeUnit.Day, @"d(ays?)?" },
				{ TimeUnit.Week, @"w((ee)?ks?)?" },
				{ TimeUnit.Month, @"mon(ths?)?" },
				{ TimeUnit.Year, @"y((ea)?rs?)?" },
				{ TimeUnit.Century, @"centur(y|ies)" },
				{ TimeUnit.Millenium, @"millenn?i(um|a)" }
			};

			foreach (KeyValuePair<TimeUnit, string> unit in regExps)
			{
				string parsable = Regex.Match(input, $@"[0-9.]+\s*({unit.Value})(([0-9]+)|\s|$)", RegexOptions.IgnoreCase).Value;
				if (string.IsNullOrEmpty(parsable)) continue;
				for (int i = 0; i < parsable.Length; i++)
				{
					if (!char.IsDigit(parsable[i]))
					{
						if (!double.TryParse(parsable[..i], out double factor))
						{
							error = $"Failed to parse number \"${parsable[..i]}\" for unit ${unit.Key}.";
							return false;
						}
						try
						{
							span = span.Add(factor * UnitToTime[unit.Key]);
						}
						catch (OverflowException e)
						{
							error = $"Unable to create time! The duration you specified is too long.\n[{e.Message}]";
							return false;
						}
						break;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Represents a range of time units ranging from MILLISECOND to MILLENIUM.
		/// </summary>
		public enum TimeUnit
		{
			/// <summary>
			/// Represents a thousandth of a second
			/// </summary>
			Millisecond,
			/// <summary>
			/// Represents a second
			/// </summary>
			Second,
			/// <summary>
			/// Represents a minute (60 seconds)
			/// </summary>
			Minute,
			/// <summary>
			/// Represents an hour (60 minutes)
			/// </summary>
			Hour,
			/// <summary>
			/// Represents a day (24 hours)
			/// </summary>
			Day,
			/// <summary>
			/// Represents a week (7 days)
			/// </summary>
			Week,
			/// <summary>
			/// Represents a month (30 days)
			/// </summary>
			Month,
			/// <summary>
			/// Represents a year (365.24 days)
			/// </summary>
			Year,
			/// <summary>
			/// Represents a century (100 years)
			/// </summary>
			Century,
			/// <summary>
			/// Represents a millenium (1000 years)
			/// </summary>
			Millenium
		}

		/// <summary>
		/// Gets the appropriate TimeSpan from a given TimeUnit.
		/// </summary>
		private static readonly Dictionary<TimeUnit, TimeSpan> UnitToTime = new()
		{
			{ TimeUnit.Millisecond, TimeSpan.FromMilliseconds(1)},
			{ TimeUnit.Second, TimeSpan.FromSeconds(1)},
			{ TimeUnit.Minute, TimeSpan.FromMinutes(1)},
			{ TimeUnit.Hour, TimeSpan.FromHours(1)},
			{ TimeUnit.Day, TimeSpan.FromDays(1)},
			{ TimeUnit.Week, TimeSpan.FromDays(7)},
			{ TimeUnit.Month, TimeSpan.FromDays(30)},
			{ TimeUnit.Year, TimeSpan.FromDays(365.24)},
			{ TimeUnit.Century, TimeSpan.FromDays(36524)},
			{ TimeUnit.Millenium, TimeSpan.FromDays(365240)}
		};

		/// <summary>
		/// Searches the list of static time zone abbreviations in <paramref name="config"/> to find the closest expressions to <paramref name="input"/>.
		/// </summary>
		/// <param name="input">The search term.</param>
		/// <param name="config">The global language configuration from which to draw time zone abbreviations.</param>
		/// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="config"/> which are most similar to <paramref name="input"/>, sorted by relevance.</returns>

		public static string[] SearchTimeZone(this string input, LanguageConfiguration config)
		{
			Dictionary<string, int> searchWeight = new();

			foreach (KeyValuePair<string, TimeZoneData> k in config.TimeZones)
			{
				int weight = 0;
				for (int i = 0; i < k.Key.Length; i++)
				{
					if (i < input.Length)
					{
						if (input[i] == k.Key[i]) weight += 10;
						else if (char.ToUpper(input[i]) == char.ToUpper(k.Key[i])) weight += 9;
					}
					if (input.Contains(k.Key[i])) weight += 3;
				}

				if (input.Length >= 2 && input[^2] == 'S')
				{
					if (k.Key.Length >= 2 && k.Key[^2] == 'D') weight += 8;
					else if (k.Key.Length + 1 == input.Length) weight += 4;
				}

				if (k.Key.Length >= 2 && k.Key[^2] == 'S')
				{
					if (input.Length >= 2 && input[^2] == 'D') weight += 8;
					else if (input.Length + 1 == k.Key.Length) weight += 4;
				}

				if (input.ToUpper() == k.Key.ToUpper()) weight += 100;

				searchWeight.Add(k.Key, weight);
			}

			List<KeyValuePair<string, int>> weightedList = searchWeight.ToList();
			weightedList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

			string[] sortedResults = new string[weightedList.Count];
			for (int i = 0; i < weightedList.Count; i++)
			{
				sortedResults[i] = weightedList[i].Key;
			}
			return sortedResults;
		}

		/// <summary>
		/// Searches the list of static time zone data in <paramref name="config"/> to find timezones whose offset is closest to <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">A TimeSpan representing the difference between UTC and a given time zone.</param>
		/// <param name="config">The configuration required to parse time zone data.</param>
		/// <param name="exactMatches">The number of matches in the results that have the exact <paramref name="offset"/> provided.</param>
		/// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="config"/> which are most similar in offset to <paramref name="offset"/>, sorted by relevance.</returns>

		public static string[] SearchTimeZone(this TimeSpan offset, LanguageConfiguration config, out int exactMatches)
		{
			Dictionary<string, int> searchWeight = new();
			exactMatches = 0;

			foreach (KeyValuePair<string, TimeZoneData> k in config.TimeZones)
			{
				int weight = (int)Math.Abs(k.Value.TimeOffset.Subtract(offset).TotalMinutes);

				if (weight == 0) exactMatches++;

				searchWeight.Add(k.Key, weight);
			}

			List<KeyValuePair<string, int>> weightedList = searchWeight.ToList();
			weightedList.Sort((pair1, pair2) =>
			{
				if (pair1.Value.CompareTo(pair2.Value) != 0) return pair1.Value.CompareTo(pair2.Value);
				else return pair1.Key.CompareTo(pair2.Key);
			});

			string[] sortedResults = new string[weightedList.Count];
			for (int i = 0; i < weightedList.Count; i++)
			{
				sortedResults[i] = weightedList[i].Key;
			}
			return sortedResults;
		}

		/// <summary>
		/// Finds the longest common substring between <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">The first string.</param>
		/// <param name="b">The second string.</param>
		/// <returns>The length of the longest common substring found.</returns>

		public static int LongestCommonSubstr(string a, string b)
		{
			return LongestCommonSubstr(a, b, a.Length, b.Length);
		}

		private static int LongestCommonSubstr(string a, string b, int aimax, int bimax)
		{
			int[,] LCSuffix = new int[aimax + 1, bimax + 1];
			int result = 0;

			for (int i = 0; i <= aimax; i++)
			{
				for (int j = 0; j <= bimax; j++)
				{
					if (i == 0 || j == 0)
						LCSuffix[i, j] = 0;
					else if (a[i - 1] == b[j - 1])
					{
						LCSuffix[i, j] = LCSuffix[i - 1, j - 1] + 1;

						if (LCSuffix[i, j] > result) result = LCSuffix[i, j];
					}
					else
						LCSuffix[i, j] = 0;
				}
			}

			return result;
		}

		private const double LengthWeight = 0.015;
		private const double MaxSubstringWeight = 0.9;
		private const double PositionalCorrelationWeight = 0.05;
		private const double CountCorrelationWeight = 0.035;

		/// <summary>
		/// Obtains an index detailing how closely related <paramref name="a"/> and <paramref name="b"/> are based on a series of parameters.
		/// </summary>
		/// <param name="a">The first string to compare</param>
		/// <param name="b">The second string to compare</param>
		/// <returns>A decimal number in the range [0..1] indicating how closely correlated <paramref name="a"/> and <paramref name="b"/> are.</returns>

		public static double GetCorrelationIndex(string a, string b)
		{
			double n = Math.Max(a.Length, b.Length);

			double pLength = Math.Min(a.Length, b.Length) / n;
			int LCSS = LongestCommonSubstr(a, b);
			double pMaxSubstr = LCSS * LCSS / n;
			if (pMaxSubstr > 0.9)
			{
				pMaxSubstr = 0.9 + 0.1 * (LCSS / n);
			}

			Dictionary<char, PairwiseCounter> counts = new();
			for (int i = 0; i < a.Length; i++)
			{
				if (!counts.ContainsKey(a[i]))
				{
					counts.Add(a[i], new PairwiseCounter());
				}
				counts[a[i]].count1++;
			}
			for (int j = 0; j < b.Length; j++)
			{
				if (!counts.ContainsKey(b[j]))
				{
					counts.Add(b[j], new PairwiseCounter());
				}
				counts[b[j]].count2++;
			}
			double pCounts = 0;
			foreach (PairwiseCounter c in counts.Values)
			{
				pCounts += c.GetCorrelationStrength();
			}
			pCounts /= counts.Count;

			int posCount = 0;
			for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
			{
				if (a[i] == b[i])
					posCount++;
			}
			double pPos = posCount / n;

			return pLength * LengthWeight + pMaxSubstr * MaxSubstringWeight + pPos * PositionalCorrelationWeight + pCounts * CountCorrelationWeight;
		}

		internal class PairwiseCounter
		{
			public int count1 = 0;
			public int count2 = 0;
			public double GetCorrelationStrength()
			{
				return Math.Min(count1, count2) / (double)Math.Max(count1, count2);
			}

			public PairwiseCounter(int count1 = 0, int count2 = 0)
			{
				this.count1 = count1;
				this.count2 = count2;
			}
		}

		/// <summary>
		/// Creates a standard expression of a specific time, both absolute and relative to present.
		/// </summary>
		/// <param name="time">The DateTimeOffset object to parse.</param>
		/// <param name="config">The Configuration file holding the StandardTimeZone variable, only required if <paramref name="standardizeTime"/> is <see langword="true"/>.</param>
		/// <param name="standardizeTime">Whether to standardize the time to <paramref name="config"/><c>.StandardTimeZone</c>.</param>
		/// <returns>A stringified expression of <paramref name="time"/>.</returns>

		public static string HumanizeExtended(this DateTimeOffset time, BotConfiguration config = null, bool standardizeTime = false)
		{
			if (config != null && standardizeTime)
			{
				time = time.ToOffset(TimeSpan.FromHours(config.StandardTimeZone));
			}

			return $"{time:ddd dd MMM yyy 'at' hh:mm tt 'UTC'zzz} ({time.Humanize()})";
		}

		/// <summary>
		/// Disables all here, everyone, and role mentions from a given message string, keeping the message mostly identical.
		/// </summary>
		/// <param name="input">The base string to sanitize.</param>
		/// <returns>A modified string where all instances of mass mentions have a zero-width space inserted after the @ symbol to disable them.</returns>

		public static string SanitizeMentions(this string input)
		{
			input = Regex.Replace(input, @"@here", $"@{ZWSP}here", RegexOptions.IgnoreCase);
			input = Regex.Replace(input, @"@everyone", $"@{ZWSP}everyone", RegexOptions.IgnoreCase);
			input = Regex.Replace(input, @"<@&", $"<@{ZWSP}&");
			return input;
		}

		/// <summary>
		/// A zero-width space character.
		/// </summary>

		public const char ZWSP = 'â€‹';
	}

	/// <summary>
	/// Represents a time zone for comparison as an offset to UTC.
	/// </summary>

	[Serializable]
	public class TimeZoneData
	{

		/// <summary>
		/// The Full name of the time zone.
		/// </summary>

		public string Name { get; set; }

		/// <summary>
		/// The offset to UTC of the time zone, in hours.
		/// </summary>

		public float Offset { get; set; }

		/// <summary>
		/// The offset to UTC of the time zone, as a <c>TimeSpan</c>.
		/// </summary>

		public TimeSpan TimeOffset { get { return TimeSpan.FromMinutes((int)Math.Round(Offset * 60)); } }

		/// <summary>
		/// Stringifies the given timezone
		/// </summary>
		/// <returns>A string expression of the time zone, with critical information.</returns>

		public override string ToString()
		{
			return $"{Name} | {ToTimeZoneExpression(Offset)}";
		}

		/// <summary>
		/// Gives a human-readable form of the <paramref name="offset"/> relative to UTC.
		/// </summary>
		/// <param name="offset">The number of hours offset from UTC.</param>
		/// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

		public static string ToTimeZoneExpression(float offset)
		{
			return $"UTC{(offset >= 0 ? "+" : "")}{(int)offset}:{Math.Abs(offset % 1 * 60):00}";
		}

		/// <summary>
		/// Gives a human-readable form of the <paramref name="offset"/> TimeSpan relative to UTC.
		/// </summary>
		/// <param name="offset">The TimeSpan object representing the offset of hours relative to UTC.</param>
		/// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

		public static string ToTimeZoneExpression(TimeSpan offset)
		{
			return ToTimeZoneExpression(offset.Hours + offset.Minutes / 60f);
		}

		/// <summary>
		/// Attempts to Parse a TimeZone data from given information.
		/// </summary>
		/// <param name="str">The string to parse into TimeZone data.</param>
		/// <param name="languageConfiguration">The Configuration data containing static time zone definitions.</param>
		/// <param name="result">A <c>TimeZoneData</c> object whose name is <paramref name="str"/> or the name attached to the abbreviation and Offset is obtained by parsing <paramref name="str"/></param>
		/// <returns>A <c>TimeZoneData</c> object whose name is <paramref name="str"/> and Offset is obtained by parsing <paramref name="str"/>.</returns>

		public static bool TryParse(string str, LanguageConfiguration languageConfiguration, out TimeZoneData result)
		{

			bool success = false;
			result = new TimeZoneData()
			{
				Offset = 0,
				Name = str
			};

			if (str is null)
			{
				return false;
			}

			int sign = 1;

			int signPos = str.IndexOf("+");
			if (signPos < 0)
			{
				sign = -1;
				signPos = str.IndexOf("-");
			}

			string TZString = signPos < 0 ? str : str[..signPos];
			if (!string.IsNullOrEmpty(TZString))
			{
				if (LanguageHelper.TryParseTimeZone(TZString.Trim(), languageConfiguration, out TimeZoneData TimeZone))
				{
					result.Name = TimeZone.Name;
					result.Offset = TimeZone.Offset;
					success = true;
				}
			}
			else
			{
				result.Name = "UTC";
			}

			if (signPos >= 0)
			{
				string[] mods = str[(signPos + 1)..].Split(":");
				result.Name += str[signPos] + str[(signPos + 1)..];
				result.Offset += int.Parse(mods[0]) * sign;
				if (mods.Length > 1) result.Offset += int.Parse(mods[1]) / 60f;
				success = true;
			}

			return success;
		}

		/// <summary>
		/// Holds regional indicator characters, where 0 is ðŸ‡¦ and 25 is ðŸ‡¿.
		/// </summary>
		public static readonly Dictionary<int, string> Indicators = new() {
			{0, "ðŸ‡¦"},
			{1, "ðŸ‡§"},
			{2, "ðŸ‡¨"},
			{3, "ðŸ‡©"},
			{4, "ðŸ‡ª"},
			{5, "ðŸ‡«"},
			{6, "ðŸ‡¬"},
			{7, "ðŸ‡­"},
			{8, "ðŸ‡®"},
			{9, "ðŸ‡¯"},
			{10, "ðŸ‡°"},
			{11, "ðŸ‡±"},
			{12, "ðŸ‡²"},
			{13, "ðŸ‡³"},
			{14, "ðŸ‡´"},
			{15, "ðŸ‡µ"},
			{16, "ðŸ‡¶"},
			{17, "ðŸ‡·"},
			{18, "ðŸ‡¸"},
			{19, "ðŸ‡¹"},
			{20, "ðŸ‡º"},
			{21, "ðŸ‡»"},
			{22, "ðŸ‡¼"},
			{23, "ðŸ‡½"},
			{24, "ðŸ‡¾"},
			{25, "ðŸ‡¿"}
		};
	}
}
