﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cursos.Models;
using Cursos.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using Cursos.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using PSW_Cursos.Models;

namespace Cursos.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly cursosContext context;

        public EstudiantesController(cursosContext _context)
        {
            context = _context;
        }

        [HttpGet]
        public async Task<IEnumerable<Estudiante>> Get()
        {
            return await context.Estudiante.ToListAsync();
        }

        [HttpGet("{id}", Name = "GetEstudiante")]
        public async Task<IActionResult> Get(int id, string codigo)
        {
            var estudiante = await context.Estudiante.FindAsync(id);
            if (estudiante == null)
            {
                return NotFound();
            }
            else
            {
                return Ok(estudiante);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Estudiante Estudiante)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ErrorHelper.GetModelStateErrors(ModelState));
            }
            else
            {
                Estudiante.IdEstudiante = 0;
                context.Estudiante.Add(Estudiante);
                await context.SaveChangesAsync();
                Estudiante.Codigo = "EST" + Estudiante.IdEstudiante.ToString().PadLeft(7, '0');
                await context.SaveChangesAsync();

                return CreatedAtRoute("GetEstudiante", new { id = Estudiante.IdEstudiante, Codigo = Estudiante.Codigo }, Estudiante);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Estudiante Estudiante)
        {
            if (Estudiante.IdEstudiante == 0)
            {
                Estudiante.IdEstudiante = id;
            }

            if (Estudiante.IdEstudiante != id)
            {
                return BadRequest(ErrorHelper.Response(400, "Petición no válida."));
            }

            if (!await context.Estudiante.Where(x => x.IdEstudiante == id).AsNoTracking().AnyAsync())
            {
                return NotFound();
            }
            context.Entry(Estudiante).State = EntityState.Modified;
            if (!TryValidateModel(Estudiante, nameof(Estudiante)))
            {
                return BadRequest(ErrorHelper.GetModelStateErrors(ModelState));
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("CambiarCodigo/{id}")]
        public async Task<IActionResult> CambiarCodigo(int id, [FromQuery] string codigo)
        {

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return BadRequest(ErrorHelper.Response(400, "El código está vacío."));
            }

            var Estudiante = await context.Estudiante.FindAsync(id);
            if (Estudiante == null)
            {
                return NotFound();
            }

            if (await context.Estudiante.Where(x => x.Codigo == codigo && x.IdEstudiante != id).AnyAsync())
            {
                return BadRequest(ErrorHelper.Response(400, $"El código {codigo} ya existe."));
            }

            Estudiante.Codigo = codigo;

            if (!TryValidateModel(Estudiante, nameof(Estudiante)))
            {
                return BadRequest(ErrorHelper.GetModelStateErrors(ModelState));
            }

            await context.SaveChangesAsync();
            return StatusCode(200, Estudiante);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var Estudiante = await context.Estudiante.FindAsync(id);
            if (Estudiante == null)
            {
                return NotFound();
            }

            context.Estudiante.Remove(Estudiante);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<Estudiante> _Estudiante)
        {
            var Estudiante = await context.Estudiante.FindAsync(id);
            if (Estudiante == null)
            {
                return NotFound();
            }

            _Estudiante.ApplyTo(Estudiante, (Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter)ModelState);
            if (!TryValidateModel(Estudiante, "Estudiante"))
            {
                return BadRequest(ErrorHelper.GetModelStateErrors(ModelState));
            }
            await context.SaveChangesAsync();
            return Ok(Estudiante);
        }

        [HttpGet("matricula/{estudiante}")]
        public async Task<IActionResult> MatriculasInscripciones(int estudiante)
        {
            EstudianteMatriculaInscripcionesVM Estudiante = await context.
                Estudiante.Include("Matricula.InscripcionCurso.Periodo").
                Include("Matricula.InscripcionCurso.Curso").Where(x => x.IdEstudiante == estudiante).
            Select(x => new EstudianteMatriculaInscripcionesVM()
            {
                IdEstudiante = x.IdEstudiante,
                Codigo = x.Codigo,
                Nombres = x.NombreApellido,
                Apellidos = x.Apellido,
                NombreCompleto = x.NombreApellido,
                Matriculas = x.Matricula.Select(y => new MatriculasVM()
                {
                    IdPeriodo = y.IdPeriodo,
                    Fecha = y.Fecha
                }).ToList(),
                Inscripciones = x.Matricula.SelectMany(z => z.InscripcionCurso).Select(a => new InscripcionesVM()
                {
                    IdEstudiante = x.IdEstudiante,
                    Anio = a.Periodo.Anio,
                    Codigo = a.Curso.Codigo,
                    Descripcion = a.Curso.Descripcion
                }).ToList()
            }).SingleOrDefaultAsync();
            return Ok(Estudiante);
        }
    }
}