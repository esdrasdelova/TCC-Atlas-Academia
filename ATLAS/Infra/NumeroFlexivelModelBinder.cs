using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ATLAS.Infra;

/// <summary>
/// Binder de números "flexível": aceita tanto ponto quanto vírgula como
/// separador decimal. Os &lt;input type="number"&gt; do HTML enviam sempre com
/// ponto (ex.: "78.5"), mas um usuário pode digitar "78,5" em campos de texto.
/// Sem isto, um servidor em cultura pt-BR interpretaria "78.5" como 785.
/// </summary>
public class NumeroFlexivelModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var tipo = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        if (tipo == typeof(decimal) || tipo == typeof(double) || tipo == typeof(float))
        {
            return new NumeroFlexivelModelBinder(tipo);
        }

        return null;
    }
}

public class NumeroFlexivelModelBinder : IModelBinder
{
    private readonly Type _tipo;

    public NumeroFlexivelModelBinder(Type tipo) => _tipo = tipo;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valor = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valor == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valor);
        var texto = valor.FirstValue;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return Task.CompletedTask;
        }

        texto = texto.Trim().Replace(" ", string.Empty);

        // Heurística: se há vírgula e nenhum ponto, a vírgula é o decimal.
        // Se há ambos ("1.234,56"), o ponto é milhar e a vírgula é o decimal.
        // Se há só ponto, ele é o decimal.
        string normalizado;
        if (texto.Contains(',') && texto.Contains('.'))
        {
            normalizado = texto.Replace(".", string.Empty).Replace(',', '.');
        }
        else
        {
            normalizado = texto.Replace(',', '.');
        }

        if (decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out var numero))
        {
            object convertido = _tipo == typeof(decimal) ? numero
                : _tipo == typeof(double) ? (double)numero
                : (float)numero;

            bindingContext.Result = ModelBindingResult.Success(convertido);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"O valor '{texto}' não é um número válido.");
        }

        return Task.CompletedTask;
    }
}
