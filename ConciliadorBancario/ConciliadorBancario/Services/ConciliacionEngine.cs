using System;
using System.Collections.Generic;
using System.Linq;
using ConciliadorBancario.Data;
using ConciliadorBancario.Models;

namespace ConciliadorBancario.Services
{
    public class ConciliacionEngine
    {
        private readonly MovimientoRepository _movRepo;

        public ConciliacionEngine()
        {
            _movRepo = new MovimientoRepository();
        }

        public void AutoConciliar()
        {
            var bancos = _movRepo.GetPendientes("Banco");
            var sistemas = _movRepo.GetPendientes("Sistema");

            foreach (var banco in bancos)
            {
                if (banco.Estado == EstadosMovimiento.PendienteAsientoMasivo) continue;

                // Regular match exacto (misma fecha, mismo monto)
                var match = sistemas.FirstOrDefault(s =>
                    Math.Abs(s.Monto - banco.Monto) < 0.01 &&
                    s.Fecha.Date == banco.Fecha.Date &&
                    s.Estado == EstadosMovimiento.NoEncontrado);

                if (match != null)
                {
                    MarcarConciliado(banco, match, "Auto Match Exacto");
                    sistemas.Remove(match);
                }
            }

            // Asientos Masivos Match
            // User requested EXACT date match for AM because they are generated with the exact date from the bank
            bancos = _movRepo.GetPendientes("Banco");
            var masivosSistema = sistemas.Where(s => s.Concepto != null && s.Concepto.Contains("[AM-")).ToList();

            foreach(var sys in masivosSistema)
            {
                var bancoMatch = bancos.FirstOrDefault(b =>
                    b.Estado == EstadosMovimiento.PendienteAsientoMasivo &&
                    Math.Abs(sys.Monto - b.Monto) < 0.01 &&
                    b.Fecha.Date == sys.Fecha.Date); // EXACT Date match

                if(bancoMatch != null)
                {
                    MarcarConciliado(bancoMatch, sys, "Auto Match Asiento Masivo");
                    bancos.Remove(bancoMatch);
                }
            }
        }

        public List<Movimiento> ObtenerCandidatosFuzzy(Movimiento origen, List<Movimiento> posibles)
        {
            // Fuzzy match for MANUAL linking.
            // Allows up to 7 days difference for same amount (e.g. accounting entry delayed from bank date).
            // Or same date but up to 1 dollar difference.
            return posibles.Where(p =>
                p.Estado == EstadosMovimiento.NoEncontrado &&
                ((Math.Abs(p.Monto - origen.Monto) < 0.01 && Math.Abs((p.Fecha - origen.Fecha).TotalDays) <= 7) ||
                 (Math.Abs(p.Monto - origen.Monto) <= 1.00 && p.Fecha.Date == origen.Fecha.Date))
                )
                .OrderBy(p => Math.Abs(p.Monto - origen.Monto))
                .ThenBy(p => Math.Abs((p.Fecha - origen.Fecha).TotalDays))
                .ToList();
        }

        public void MarcarConciliado(Movimiento banco, Movimiento sistema, string obs)
        {
            _movRepo.UpdateEstado(banco.Id, EstadosMovimiento.Conciliado, obs, sistema.Id);
            _movRepo.UpdateEstado(sistema.Id, EstadosMovimiento.Conciliado, obs, banco.Id);
        }
    }
}
