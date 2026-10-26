const formatoPesos = new Intl.NumberFormat('es-DO', {
  style: 'currency',
  currency: 'DOP',
  currencyDisplay: 'narrowSymbol',
  maximumFractionDigits: 0,
})

/** 800 → "RD$800" (el símbolo depende del navegador, el número siempre sin decimales). */
export function formatearPrecio(monto: number): string {
  const texto = formatoPesos.format(monto)
  return texto.startsWith('$') ? `RD${texto}` : texto
}
