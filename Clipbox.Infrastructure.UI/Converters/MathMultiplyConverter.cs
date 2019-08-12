namespace Clipbox.Infrastructure.UI.Converters
{
    public class MathMultiplyConverter : MathConverterBase
    {
        protected override double DoMathOperation(double number1, double number2)
        {
            return (number1 == 0 ? 1 : number1) * (number2 == 0 ? 1 : number2);
        }
    }
}

