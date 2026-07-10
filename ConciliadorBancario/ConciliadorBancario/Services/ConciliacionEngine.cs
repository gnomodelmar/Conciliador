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
                if (banco.Estado == EstadosMovimiento.PendienteAsientoMasivo) continue; // Will be matched next week

                // Exact match: Same Amount, Same Date. Operation code check if available.
                var match = sistemas.FirstOrDefault(s =>
                    s.Monto == banco.Monto &&
                    s.Fecha.Date == banco.Fecha.Date &&
                    s.Estado == EstadosMovimiento.NoEncontrado);

                if (match != null)
                {
                    MarcarConciliado(banco, match, "Auto Match Exacto");
                    sistemas.Remove(match); // Prevents matching same system mov twice
                }
            }

            // Cross Match specific logic for Asientos Masivos already in System
            bancos = _movRepo.GetPendientes("Banco");
            var masivosSistema = sistemas.Where(s => s.Concepto != null && s.Concepto.Contains("[AM-")).ToList();

            foreach(var sys in masivosSistema)
            {
                // Find all bancarios that are Pendiente Asiento Masivo matching this amount total?
                // Or maybe the user generated details rows exactly matching the AM.
                // If it's detailed rows, we can just match by exact amount and some keywords.
                var bancoMatch = bancos.FirstOrDefault(b =>
                    b.Estado == EstadosMovimiento.PendienteAsientoMasivo &&
                    b.Monto == sys.Monto &&
                    Math.Abs((b.Fecha - sys.Fecha).TotalDays) <= 3);

                if(bancoMatch != null)
                {
                    MarcarConciliado(bancoMatch, sys, "Auto Match Asiento Masivo");
                    bancos.Remove(bancoMatch);
                }
            }
        }

        public List<Movimiento> ObtenerCandidatosFuzzy(Movimiento origen, List<Movimiento> posibles)
        {
            // Tolerance: +- 0.50 amount, +- 3 days
            return posibles.Where(p =>
                p.Estado == EstadosMovimiento.NoEncontrado &&
                Math.Abs(p.Monto - origen.Monto) <= 0.50 &&
                Math.Abs((p.Fecha - origen.Fecha).TotalDays) <= 3)
                .OrderBy(p => Math.Abs(p.Monto - origen.Monto))
                .ToList();
        }

        public void MarcarConciliado(Movimiento banco, Movimiento sistema, string obs)
        {
            _movRepo.UpdateEstado(banco.Id, EstadosMovimiento.Conciliado, obs, sistema.Id);
            _movRepo.UpdateEstado(sistema.Id, EstadosMovimiento.Conciliado, obs, banco.Id);
        }
    }
}
