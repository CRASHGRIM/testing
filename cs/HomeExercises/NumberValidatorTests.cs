﻿using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace HomeExercises
{
    [TestFixture]
    public class NumberValidatorTests
	{
        [TestCase(-1, 2, true, TestName = "NegativePrecision_ShouldThrowArgumentException")]
        [TestCase(1, -1, true, TestName = "NegativeScale_ShouldThrowArgumentException")]
        [TestCase(1, 2, true, TestName = "ScaleLessThanPrecision_ShouldThrowArgumentException")]
        public void Creation_Throws(int precision, int scale, bool onlyPositive)
        {
            Action ValidatorCreation = () => new NumberValidator(precision, scale, onlyPositive);
            ValidatorCreation.ShouldThrow<ArgumentException>();
        }

        [TestCase(2, 1, true, TestName = "PositiveScaleAndPrecision_ShouldNotThrowException")]
        [TestCase(100, 30, true, TestName = "PositiveScaleAndPrecision_ShouldNotThrowException")]
        [TestCase(1, 0, true, TestName = "ZeroScale_ShouldNotThrowException")]
        public void Creation_ShouldNotThrows(int precision, int scale, bool onlyPositive)
        {
            Action ValidatorCreation = () => new NumberValidator(precision, scale, onlyPositive);
            ValidatorCreation.ShouldNotThrow<ArgumentException>();
        }

        [TestCase(17, 2, true, "0.0", TestName = "dotAsDelimeter_ShouldBeValid")]
        [TestCase(17, 2, true, "0,0", TestName = "commaAsDelimeter_ShouldBeValid")]
        [TestCase(17, 2, true, "00.00", TestName = "ScaleEqualLimits_ShouldBeValid")]
        [TestCase(4, 2, true, "00.00", TestName = "PrecisionEqualLimits_ShouldBeValid")]
        [TestCase(17, 2, true, "0", TestName = "OnlyIntPart_ShouldBeValid")]
        [TestCase(10, 5, false, "+1.0", TestName = "PlusAsSign_ShouldBeValid")]
        [TestCase(10, 5, false, "-1.0", TestName = "InputNegative_ShouldBeValidIfValidatorAcceptNegative")]
        [TestCase(30, 15, true, "00000000000000.00000000000000", TestName = "NumbersGreaterThanInt_ShouldBeValid")]
        [TestCase(10, 5, false, "0001.0001", TestName = "excessZeroesInIntPart_ShouldBeValid")]
        [TestCase(10, 5, false, "0.10000", TestName = "excessZeroesInFracPart_ShouldBeValid")]
        public void IsNumberValid_ValidCases(int precision, int scale, bool onlyPositive, string number)
        {
            var validator = new NumberValidator(precision, scale, onlyPositive);
            validator.IsValidNumber(number).Should().BeTrue();
        }

        [TestCase(17, 2, true, "", TestName = "EmptyInput_ShouldNotBeValid")]
        [TestCase(10, 5, true, null, TestName = "NullInput_ShouldNotBeValid")]
        [TestCase(3, 2, true, "111111", TestName = "OnlyIntPartGreaterThanScale_ShouldNotBeValid")]
        [TestCase(3, 2, true, "00.00", TestName = "ScaleGreaterThanLimits_ShouldNotBeValid")]
        [TestCase(17, 2, true, "0.000", TestName = "PrecisionGreaterThanLimits_ShouldNotBeValid")]
        [TestCase(17, 2, true, "-1.0", TestName = "InputNegative_ShouldNotBeValidIfValidatorNotAcceptNegative")]
        [TestCase(10, 5, false, "1.0.0", TestName = "TwoDelimeters_ShouldNotBeValid")]
        [TestCase(10, 5, true, "a.sd", TestName = "NotNumbers_ShouldNotBeValid")]
        [TestCase(10, 5, true, "ф.ыв", TestName = "NotNumbers_ShouldNotBeValid")]
        [TestCase(10, 5, true, "\n.\n\n", TestName = "NotNumbers_ShouldNotBeValid")]
        [TestCase(10, 5, true, "1.23asd", TestName = "IfContainLetters_ShouldNotBeValid")]
        [TestCase(10, 5, false, "-+1.23", TestName = "MultipleSign_ShouldNotBeValid")]
        [TestCase(10, 5, false, "1.-23", TestName = "SignInFracPart_ShouldNotBeValid")]
        public void IsNumberValid_NotValidCases(int precision, int scale, bool onlyPositive, string number)
        {
            var validator = new NumberValidator(precision, scale, onlyPositive);
            validator.IsValidNumber(number).Should().BeFalse();
        }

        [TestCase(3, 2, true, "+0.00", ExpectedResult = false, TestName = "SignShouldBePartOfScale")]
        [TestCase(4, 2, true, "+0.00", ExpectedResult = true, TestName = "SignShouldBePartOfScale")]
        public bool NumberWithSign_SignShouldBePartOfScale(int precision, int scale, bool onlyPositive, string number)
        {
            return new NumberValidator(precision, scale, onlyPositive).IsValidNumber(number);
        }

        [TestCase(17, 10, true, 17, 10, false, "-1", TestName = "CheckingThatOnlyPositiveNotStatic")]
        [TestCase(5, 3, true, 10, 3, true, "000000.0", TestName = "CheckingThatScaleNotStatic")]
        [TestCase(10, 3, true, 10, 8, true, "1.000000", TestName = "CheckingThatPrecisionNotStatic")]
        public void CheckingThatClassNotStatic(int precision1, int scale1, bool onlyPositive1, int precision2, int scale2, bool onlyPositive2, string number)
        {
            var val1 = new NumberValidator(precision1, scale1, onlyPositive1);
            var val2 = new NumberValidator(precision2, scale2, onlyPositive2);
            val1.IsValidNumber(number).Should().BeFalse();
        }
    }

	public class NumberValidator
	{
		private readonly Regex numberRegex;
		private readonly bool onlyPositive;
		private readonly int precision;
		private readonly int scale;

		public NumberValidator(int precision, int scale = 0, bool onlyPositive = false)
		{
			this.precision = precision;
			this.scale = scale;
			this.onlyPositive = onlyPositive;
			if (precision <= 0)
				throw new ArgumentException("precision must be a positive number");
			if (scale < 0 || scale >= precision)
				throw new ArgumentException("precision must be a non-negative number less or equal than precision");
			numberRegex = new Regex(@"^([+-]?)(\d+)([.,](\d+))?$", RegexOptions.IgnoreCase);
		}

		public bool IsValidNumber(string value)
		{
			// Проверяем соответствие входного значения формату N(m,k), в соответствии с правилом, 
			// описанным в Формате описи документов, направляемых в налоговый орган в электронном виде по телекоммуникационным каналам связи:
			// Формат числового значения указывается в виде N(m.к), где m – максимальное количество знаков в числе, включая знак (для отрицательного числа), 
			// целую и дробную часть числа без разделяющей десятичной точки, k – максимальное число знаков дробной части числа. 
			// Если число знаков дробной части числа равно 0 (т.е. число целое), то формат числового значения имеет вид N(m).

			if (string.IsNullOrEmpty(value))
				return false;

			var match = numberRegex.Match(value);
			if (!match.Success)
				return false;

			// Знак и целая часть
			var intPart = match.Groups[1].Value.Length + match.Groups[2].Value.Length;
			// Дробная часть
			var fracPart = match.Groups[4].Value.Length;

			if (intPart + fracPart > precision || fracPart > scale)
				return false;

			if (onlyPositive && match.Groups[1].Value == "-")
				return false;
			return true;
		}
	}
}