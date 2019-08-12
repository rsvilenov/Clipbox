namespace Clipbox.Infrastructure.UI.Converters
{
    public class MathAddConverter : MathConverterBase
    {
        protected override double DoMathOperation(double number1, double number2)
        {
            return number1 + number2;
        }
    }
}
