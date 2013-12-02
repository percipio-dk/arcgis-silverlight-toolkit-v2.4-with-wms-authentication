using System;

namespace ESRI.ArcGIS.Client.Toolkit.Utilities
{	
	/// <summary>
	/// *FOR INTERNAL USE ONLY* The RangeDomainValidator class.
	/// </summary>
	/// <remarks>Used by property setters of the fields having range domain information.</remarks>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public class RangeDomainValidator
	{
		/// <summary>
		/// Gets or sets the min value of the range domain.
		/// </summary>
		/// <value>The min value.</value>
		public static object minValue { get; set; }
		/// <summary>
		/// Gets or sets the max valueof the range domain.
		/// </summary>
		/// <value>The max value.</value>
		public static object maxValue { get; set; }

		private static string errorMessage = Properties.Resources.Validation_InvalidRangeDomain;   // Error message shown after validation fails 

		/// <summary>
		/// Determines whether the nullable byte value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(byte? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the byte value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(byte value)
		{
			byte lowerBound;
			byte upperBound;
			if (byte.TryParse(minValue.ToString(), out lowerBound) &&
				byte.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable short integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(short? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the short integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(short value)
		{
			short lowerBound;
			short upperBound;
			if (short.TryParse(minValue.ToString(), out lowerBound) &&
				short.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(int? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(int value)
		{
			int lowerBound;
			int upperBound;
			if (int.TryParse(minValue.ToString(), out lowerBound) &&
				int.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable long integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(long? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the long integer value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(long value)
		{
			long lowerBound;
			long upperBound;
			if (long.TryParse(minValue.ToString(), out lowerBound) &&
				long.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable DateTime value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(DateTime? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the DateTime value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(DateTime value)
		{
			DateTime lowerBound;
			DateTime upperBound;
			if (DateTime.TryParse(minValue.ToString(), out lowerBound) &&
				DateTime.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value.Ticks < lowerBound.Ticks || value.Ticks > upperBound.Ticks)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable single value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(Single? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the single value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(Single value)
		{
			Single lowerBound;
			Single upperBound;
			if (Single.TryParse(minValue.ToString(), out lowerBound) &&
				Single.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable decimal value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(decimal? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the decimal value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(decimal value)
		{
			decimal lowerBound;
			decimal upperBound;
			if (decimal.TryParse(minValue.ToString(), out lowerBound) &&
				decimal.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
		/// <summary>
		/// Determines whether the nullable double value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(double? value)
		{
			if (!value.HasValue) return;
			IsInValidRange(value.Value);
		}
		/// <summary>
		/// Determines whether the double value is in valid range.
		/// </summary>
		/// <param name="value">The value.</param>
		public static void IsInValidRange(double value)
		{
			double lowerBound;
			double upperBound;
			if (double.TryParse(minValue.ToString(), out lowerBound) &&
				double.TryParse(maxValue.ToString(), out upperBound))
			{
				if (value < lowerBound || value > upperBound)
					throw new ArgumentException(string.Format(errorMessage, minValue, maxValue));
			}
		}
	}
}
