namespace Dexter.Abstractions
{
	public abstract class Event : Service
	{
		/// <summary>
		/// The Initialize abstract method is what is called when all dependencies are initialized.
		/// It can be used to hook into delegates to run when an event occurs.
		/// </summary>
		public abstract void InitializeEvents();

	}
}
