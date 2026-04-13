using FluentValidation;
using FluentValidation.Results;

namespace Collectibles.Application.Tests.Helpers;

public static class ValidationTestHelper
{
    public static async Task<ValidationResult> ValidateAsync<T>(IValidator<T> validator, T instance)
    {
        return await validator.ValidateAsync(instance);
    }

    public static void ShouldHaveValidationErrorFor<T, TProperty>(
        IValidator<T> validator,
        T instance,
        System.Linq.Expressions.Expression<Func<T, TProperty>> propertyExpression,
        string? expectedErrorMessage = null)
    {
        var result = validator.Validate(instance);
        var propertyName = GetPropertyName(propertyExpression);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == propertyName);

        if (!string.IsNullOrEmpty(expectedErrorMessage))
        {
            result.Errors
                .Where(error => error.PropertyName == propertyName)
                .Should().Contain(error => error.ErrorMessage == expectedErrorMessage);
        }
    }

    public static void ShouldNotHaveValidationErrorFor<T, TProperty>(
        IValidator<T> validator,
        T instance,
        System.Linq.Expressions.Expression<Func<T, TProperty>> propertyExpression)
    {
        var result = validator.Validate(instance);
        var propertyName = GetPropertyName(propertyExpression);

        result.Errors.Should().NotContain(error => error.PropertyName == propertyName);
    }

    private static string GetPropertyName<T, TProperty>(
        System.Linq.Expressions.Expression<Func<T, TProperty>> expression)
    {
        var memberExpression = expression.Body as System.Linq.Expressions.MemberExpression;
        if (memberExpression == null)
        {
            var unaryExpression = expression.Body as System.Linq.Expressions.UnaryExpression;
            memberExpression = unaryExpression?.Operand as System.Linq.Expressions.MemberExpression;
        }

        return memberExpression?.Member.Name ?? string.Empty;
    }
}
