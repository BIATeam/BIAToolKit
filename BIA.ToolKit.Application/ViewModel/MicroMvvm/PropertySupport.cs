namespace BIA.ToolKit.Application.ViewModel.MicroMvvm
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class PropertySupport
    {
        public static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpresssion)
        {
            ArgumentNullException.ThrowIfNull(propertyExpresssion);

            if (propertyExpresssion.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("The expression is not a member access expression.", nameof(propertyExpresssion));
            }

            PropertyInfo property = memberExpression.Member as PropertyInfo ?? throw new ArgumentException("The member access expression does not access a property.", nameof(propertyExpresssion));
            MethodInfo getMethod = property.GetGetMethod(true);
            if (getMethod.IsStatic)
            {
                throw new ArgumentException("The referenced property is a static property.", nameof(propertyExpresssion));
            }

            return memberExpression.Member.Name;
        }
    }
}
