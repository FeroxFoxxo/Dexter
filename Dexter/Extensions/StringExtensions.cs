using Dexter.Configurations;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Extensions
{

	/// <summary>
	/// The String Extensions class offers a variety of different extensions that can be applied to a string to modify it.
	/// These include the prettify, sanitize and markdown extensions.
	/// </summary>

	public static class StringExtensions
	{

		private static readonly string[] SensitiveCharacters = { "\\", "*", "_", "~", "`", "|", ">", "[", "(" };

		/// <summary>
		/// The Prettify method removes all the characters before the name of the class and only selects characters from A-Z.
		/// </summary>
		/// <param name="name">The string you wish to run through the REGEX expression.</param>
		/// <returns>A sanitised string with the characters before the name of the class removed.</returns>

		public static string Prettify(this string name) => Regex.Replace(name, @"(?<!^)(?=[A-Z])", " ");

		/// <summary>
		/// The Sanitize method removes the "Commands" string from the name of the class.
		/// </summary>
		/// <param name="name">The string you wish to run through the replace method.</param>
		/// <returns>The name of a module with the "Commands" string removed.</returns>

		public static string Sanitize(this string name) => name.Replace("Commands", string.Empty);

		/// <summary>
		/// The Sanitize Markdown method removes any sensitive characters that may otherwise change the created embed.
		/// It does this by looping through and replacing any sensitive characters that may break the embed.
		/// </summary>
		/// <param name="text">The string you wish to be run through the command.</param>
		/// <returns>The text which has been sanitized and has had the sensitive characters removed.</returns>

		public static string SanitizeMarkdown(this string text)
		{
			foreach (string Unsafe in SensitiveCharacters)
				text = text.Replace(Unsafe, $"\\{Unsafe}");
			return text;
		}

		public static string HumanizeTimeSpan(this TimeSpan t)
		{
			return t.Humanize(3, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Day);
		}

		public static LogLevel ToLogLevel(this LogSeverity severity)
		{
			return severity switch
			{
				LogSeverity.Critical => LogLevel.Critical,
				LogSeverity.Error => LogLevel.Error,
				LogSeverity.Warning => LogLevel.Warning,
				LogSeverity.Info => LogLevel.Information,
				LogSeverity.Verbose => LogLevel.Trace,
				LogSeverity.Debug => LogLevel.Debug,
				_ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
			};
		}

		/// <summary>
		/// Gets the class of the last method that had been called.
		/// </summary>
		/// <param name="searchHeight">The height backwards that you would like to see the call come from.</param>
		/// <returns>The last called class + method</returns>

		public static KeyValuePair<string, string> GetLastMethodCalled(int searchHeight)
		{
			searchHeight += 1;

			Type mBase = new StackTrace().GetFrame(searchHeight).GetMethod().DeclaringType;

			//Console.Out.WriteLine(mBase.FullName);

			if (mBase.Assembly != Assembly.GetExecutingAssembly() || mBase.Namespace == typeof(StringExtensions).Namespace)
				return GetLastMethodCalled(searchHeight + 1);

			string name;

			if (mBase.DeclaringType != null)
				name = mBase.DeclaringType.Name;
			else
				name = mBase.Name;

			string methodName = mBase.Name;

			int Index = methodName.IndexOf(">d__");

			if (Index != -1)
				methodName = methodName.Substring(0, Index).Replace("<", "");

			return new KeyValuePair<string, string>(name, methodName);
		}

		public static object SetClassParameters(this object newClass, IServiceScope scope, IServiceProvider sp)
		{
			newClass.GetType().GetProperties().ToList().ForEach(property =>
			{
				if (property.PropertyType == typeof(IServiceProvider))
					property.SetValue(newClass, sp);
				else
				{
					object service = scope.ServiceProvider.GetService(property.PropertyType);

					if (service != null)
					{
						property.SetValue(newClass, service);
					}
				}
			});

			return newClass;
		}

		/// <summary>
		/// Obtains a Proxied URL from a given Image URL.
		/// </summary>
		/// <param name="imageURL">The URL of the target image.</param>
		/// <param name="imageName">The Name to give the image once downloaded.</param>
		/// <param name="client">A Discord Socket Client service to parse the storage channel.</param>
		/// <param name="config">Configuration holding the storage channel ID.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public static async Task<string> GetProxiedImage(this string imageURL, string imageName, DiscordShardedClient client, ProposalConfiguration config)
		{
			string imageCacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");

			if (!Directory.Exists(imageCacheDir))
				Directory.CreateDirectory(imageCacheDir);

			string filePath = Path.Combine(imageCacheDir, $"{imageName}{Path.GetExtension(imageURL.Split("?")[0])}");

			using HttpClient httpClient = new();

			var response = await httpClient.GetAsync(imageURL);

			using (var fs = new FileStream(filePath, FileMode.CreateNew)) {
				await response.Content.CopyToAsync(fs);
			}
			
			ITextChannel channel = client.GetChannel(config.StorageChannelID) as ITextChannel;

			IUserMessage attachmentMSG = await channel.SendFileAsync(filePath);

			File.Delete(filePath);

			return attachmentMSG.Attachments.FirstOrDefault().ProxyUrl;
		}

		/// <summary>
		/// Hashes an object into an <c>int</c> using the MD5 algorithm.
		/// </summary>
		/// <param name="hashingString">The object to Hash.</param>
		/// <returns>The hashed value as an Int32.</returns>

		public static int GetHash(this object hashingString)
		{
			return BitConverter.ToInt32(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(hashingString.ToString())));
		}

	}

}
