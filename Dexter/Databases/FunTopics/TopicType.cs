namespace Dexter.Databases.FunTopics
{

	/// <summary>
	/// [risk of deprecation] The subtype a topic falls into. Can be of type TOPIC or WOULDYOURATHER.
	/// </summary>

	public enum TopicType
	{

		/// <summary>
		/// A topic that initiates a random, unprompted conversation, generally by asking an open question.
		/// </summary>

		Topic,

		/// <summary>
		/// A topic that initiates a conversation by proposing two options and opening up a binary choice.
		/// </summary>

		WouldYouRather,

		/// <summary>
		/// A topic that states some fun, random, or unexpected fact about science, sociology, demography, or any other field of knowledge.
		/// </summary>

		FunFact,

		/// <summary>
		/// A topic that contains an introductory statement and a punchline inteded for comedy.
		/// </summary>

		Joke,

		/// <summary>
		/// A topic that contains a famous quote.
		/// </summary>

		Quote

	}

}
