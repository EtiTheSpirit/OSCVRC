using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSCVRC.DataUtils {

	/// <summary>
	/// A container of multiple value types.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	public readonly struct Variant<T1, T2, T3> {

		/// <summary>
		/// The first possible value type to store.
		/// </summary>
		public readonly T1 valueType1;

		/// <summary>
		/// The second possible value type to store.
		/// </summary>
		public readonly T2 valueType2;

		/// <summary>
		/// The third possible value type to store.
		/// </summary>
		public readonly T3 valueType3;

		/// <summary>
		/// The index of the appropriate value. This will be either 0, 1, or 2.
		/// </summary>
		public readonly int index;

		/// <summary>
		/// Store a value of type <typeparamref name="T1"/> in this <see cref="Variant{T1, T2, T3}"/>
		/// </summary>
		/// <param name="t1"></param>
		public Variant(T1 t1) {
			valueType1 = t1;
			valueType2 = default;
			valueType3 = default;
			index = 1;
		}

		/// <summary>
		/// Store a value of type <typeparamref name="T2"/> in this <see cref="Variant{T1, T2, T3}"/>
		/// </summary>
		/// <param name="t2"></param>
		public Variant(T2 t2) {
			valueType1 = default;
			valueType2 = t2;
			valueType3 = default;
			index = 2;
		}

		/// <summary>
		/// Store a value of type <typeparamref name="T3"/> in this <see cref="Variant{T1, T2, T3}"/>
		/// </summary>
		/// <param name="t3"></param>
		public Variant(T3 t3) {
			valueType1 = default;
			valueType2 = default;
			valueType3 = t3;
			index = 3;
		}

		/// <summary>
		/// Attempts to create a <see cref="Variant{T1, T2, T3}"/> from the provided <paramref name="value"/>. If the provided <paramref name="value"/> is not an instance of <typeparamref name="T1"/>, <typeparamref name="T2"/>, or <typeparamref name="T3"/>, this will raise <see cref="ArgumentException"/>.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the value is not one of the three possible types.</exception>
		public static Variant<T1, T2, T3> FromObject(object value) {
			if (value is T1 t1) {
				return new Variant<T1, T2, T3>(t1);
			} else if (value is T2 t2) {
				return new Variant<T1, T2, T3>(t2);
			} else if (value is T3 t3) {
				return new Variant<T1, T2, T3>(t3);
			} else {
				throw new ArgumentException($"The provided value is an instance of {value?.GetType().FullName ?? "null"} - this is not appropriate for this variant type, which accepts either {typeof(T1).FullName}, {typeof(T2).FullName}, or {typeof(T3).FullName}.");
			}
		}

		/// <summary>
		/// Display this as a string.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public override string ToString() {
			switch (index) {
				case 1: return valueType1?.ToString() ?? "null";
				case 2: return valueType2?.ToString() ?? "null";
				case 3: return valueType3?.ToString() ?? "null";
				default: throw new InvalidOperationException();
			};
		}

		/// <summary>
		/// Returns the hash code of the currently stored value.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public override int GetHashCode() {
			switch (index) {
				case 1: return valueType1?.GetHashCode() ?? 0;
				case 2: return valueType2?.GetHashCode() ?? 0;
				case 3: return valueType3?.GetHashCode() ?? 0;
				default: throw new InvalidOperationException();
			};
		}

		/// <summary>
		/// Determines if the provided object, which may be another instance of <see cref="Variant{T1, T2, T3}"/> or an actual instance of <typeparamref name="T1"/>, <typeparamref name="T2"/>, or <typeparamref name="T3"/> themselves, is the same as the applicable value in this object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (obj is null) return false;
			if (obj is Variant<T1, T2, T3> variant) return Equals(variant);
			if (obj is T1 t1) return Equals(t1);
			if (obj is T2 t2) return Equals(t2);
			if (obj is T3 t3) return Equals(t3);
			return false;
		}


		/// <summary>
		/// Returns true iff this variant contains the same types as the <paramref name="other"/>, and if the stored value is the same as well.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(Variant<T1, T2, T3> other) {
			if (other.index != index) return false;
			switch (index) {
				case 1: return Equals(valueType1, other.valueType1);
				case 2: return Equals(valueType2, other.valueType2);
				case 3: return Equals(valueType3, other.valueType3);
				default: throw new InvalidOperationException();
			};
		}

		/// <summary>
		/// Returns true iff this variant is storing an instance of <typeparamref name="T1"/> and if <typeparamref name="T1"/>'s <see cref="object.Equals(object?)"/> method returns <see langword="true"/>.
		/// </summry>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(T1 other) => index == 1 && Equals(valueType1, other);


		/// <summary>
		/// Returns true iff this variant is storing an instance of <typeparamref name="T2"/> and if <typeparamref name="T2"/>'s <see cref="object.Equals(object?)"/> method returns <see langword="true"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(T2 other) => index == 2 && Equals(valueType2, other);


		/// <summary>
		/// Returns true iff this variant is storing an instance of <typeparamref name="T3"/> and if <typeparamref name="T3"/>'s <see cref="object.Equals(object?)"/> method returns <see langword="true"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(T3 other) => index == 3 && Equals(valueType3, other);

		/// <inheritdoc cref="Equals(T1?)"/>
		public static explicit operator T1(Variant<T1, T2, T3> @in) => @in.index == 1 ? @in.valueType1 : throw new InvalidCastException($"This variant does not store an instance of {typeof(T1).FullName}.");

		/// <inheritdoc cref="Equals(T2?)"/>
		public static explicit operator T2(Variant<T1, T2, T3> @in) => @in.index == 2 ? @in.valueType2 : throw new InvalidCastException($"This variant does not store an instance of {typeof(T2).FullName}.");

		/// <inheritdoc cref="Equals(T3?)"/>
		public static explicit operator T3(Variant<T1, T2, T3> @in) => @in.index == 3 ? @in.valueType3 : throw new InvalidCastException($"This variant does not store an instance of {typeof(T3).FullName}.");

		/// <inheritdoc cref="Variant{T1, T2, T3}"/>
		public static implicit operator Variant<T1, T2, T3>(T1 @in) => new Variant<T1, T2, T3>(@in);

		/// <inheritdoc cref="Variant{T1, T2, T3}"/>
		public static implicit operator Variant<T1, T2, T3>(T2 @in) => new Variant<T1, T2, T3>(@in);

		/// <inheritdoc cref="Variant{T1, T2, T3}"/>
		public static implicit operator Variant<T1, T2, T3>(T3 @in) => new Variant<T1, T2, T3>(@in);


		/// <inheritdoc cref="Equals(T1?)"/>
		public static bool operator ==(Variant<T1, T2, T3> left, T1 right) => left.Equals(right);
		/// <inheritdoc cref="Equals(T2?)"/>
		public static bool operator ==(Variant<T1, T2, T3> left, T2 right) => left.Equals(right);
		/// <inheritdoc cref="Equals(T3?)"/>
		public static bool operator ==(Variant<T1, T2, T3> left, T3 right) => left.Equals(right);
		/// <inheritdoc cref="Equals(T1?)"/>
		public static bool operator ==(T1 left, Variant<T1, T2, T3> right) => right == left;
		/// <inheritdoc cref="Equals(T2?)"/>
		public static bool operator ==(T2 left, Variant<T1, T2, T3> right) => right == left;
		/// <inheritdoc cref="Equals(T3?)"/>
		public static bool operator ==(T3 left, Variant<T1, T2, T3> right) => right == left;

		/// <inheritdoc cref="Equals(Variant{T1, T2, T3})"/>
		public static bool operator ==(Variant<T1, T2, T3> left, Variant<T1, T2, T3> right) => left.Equals(right);

		/// <inheritdoc cref="Equals(T1?)"/>
		public static bool operator !=(Variant<T1, T2, T3> left, T1 right) => !(left == right);
		/// <inheritdoc cref="Equals(T2?)"/>
		public static bool operator !=(Variant<T1, T2, T3> left, T2 right) => !(left == right);
		/// <inheritdoc cref="Equals(T3?)"/>
		public static bool operator !=(Variant<T1, T2, T3> left, T3 right) => !(left == right);
		/// <inheritdoc cref="Equals(T1?)"/>
		public static bool operator !=(T1 left, Variant<T1, T2, T3> right) => !(left == right);
		/// <inheritdoc cref="Equals(T2?)"/>
		public static bool operator !=(T2 left, Variant<T1, T2, T3> right) => !(left == right);
		/// <inheritdoc cref="Equals(T3?)"/>
		public static bool operator !=(T3 left, Variant<T1, T2, T3> right) => !(left == right);

		/// <inheritdoc cref="Equals(Variant{T1, T2, T3})"/>
		public static bool operator !=(Variant<T1, T2, T3> left, Variant<T1, T2, T3> right) => !(left == right);
	}
}
