﻿using RumoPatios.Models;
using RumoPatios.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.Entity;

namespace RumoPatios.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Considere uma descarga/carregamento de 10 vgs/hora, para todos terminais
        /// </summary>
        int cargaDescarga = 10; 

        /// <summary>
        /// tempo de manobra entre linhas (em minutos)
        /// </summary>
        int tempoMovEntreLinhas = 30;
        
        /// <summary>
        /// //ou seja, tenho 5 LM
        /// </summary>
        int maxMovParalelo = 5; 

        /// <summary>
        /// //cada LM pode manobrar no maximo 60 vagoes
        /// </summary>
        int maxVagoesMov = 60; 

        //ResultadoOtimizaData result { get; set; }
        ApplicationDbContext db { get; set; }
        Random rand { get; set; }
        List<Evento> timeLine { get; set; }
        List<Tarefa> listaDeTarefas { get; set; }

        public ActionResult Index()
        {
            //TODO: criar o view model com todas as tabelas

            this.db = new ApplicationDbContext();

            var vm = new TelaPrincipal(this.db);

            return View("Index", vm);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult ExecutaCompleto()
        {
            //this.Decodificador(); //para debugar

            try
            {
                this.rand = new Random();
                this.db = new ApplicationDbContext();
                //this.db.Configuration.LazyLoadingEnabled = false;

                var k = 10;
#if !DEBUG
                k = 1000; 
#endif

                var vmList = new List<ResultadoOtimizaData>(k);
                
                for (int i = 0; i < k; i++)
                {
                    var result = new ResultadoOtimizaData();

                    result.Carregamentos = this.db.Carregamentos.Include(x => x.Linha).AsNoTracking().ToList();
                    result.Chegadas = this.db.Chegadas.AsNoTracking().ToList();
                    result.Linhas = this.db.Linhas.AsNoTracking().ToList();

                    this.geraMutante(result);
                    vmList.Add(this.Decodificador(result));
                    this.timeLine.Clear();
                    this.listaDeTarefas.Clear();
                }

                var teste = String.Join(";", vmList.Select(x => x.FO).ToList());

                vmList.Sort((x, y) => x.FO.CompareTo(y.FO));

                return View("_RespostaOtimiza", vmList[0]);
            }
            catch(Exception ex)
            {

            }

            return View();
        }

        internal void geraMutante(ResultadoOtimizaData result)
        {
            foreach(var arrival in result.Chegadas)
            {
                arrival.randLoad = 0.20 + 0.80 * (this.rand.NextDouble()); //blocos do carregamento, são de no mínimo 20%
                arrival.randUnload = 0.25 + 0.75 * (this.rand.NextDouble());
            }

            foreach(var line in result.Linhas)
            {
                line.prioridade = this.rand.NextDouble();
                //if (String.IsNullOrEmpty(line.NomeTerminal))
                //{
                //    line.vagoesCarregadosAtual = line.QtdeVagoesCarregados;
                //    line.vagoesVaziosAtual = line.QtdeVagoesVazios;
                //}
                
            }

            result.Carregamentos.ForEach(x => x.prioridade = this.rand.NextDouble());
        }

        internal ResultadoOtimizaData Decodificador(ResultadoOtimizaData result)
        {
            //this.rand = new Random();
            //this.db = new ApplicationDbContext();
            //this.result = new ResultadoOtimizaData(this.db);
            this.timeLine = new List<Evento>();
            this.listaDeTarefas = new List<Tarefa>();

            ////TODO: implementar lista generica como abaixo
            ////c# order by using reflection
            //var timeLineTest = new List<object>();
            //timeLineTest.Add(new Evento(this.rand));
            //timeLineTest.Add(new Evento(this.rand));
            //timeLineTest = timeLineTest.OrderBy(x => x.GetType().GetProperty("instante").GetValue(x)).ToList();
            //timeLineTest = timeLineTest.OrderByDescending(x => x.GetType().GetProperty("instante").GetValue(x)).ToList();

            //var carregamentos = this.db.Carregamentos.ToList();
            //var chegadas = this.db.Chegadas.ToList();
            //var linhas = this.db.Linhas.ToList();

            var linhasTerminais = result.Linhas.Where(x => String.IsNullOrEmpty(x.NomeTerminal) == false).ToList();
            var linhasDeManobra = result.Linhas.Where(x => String.IsNullOrEmpty(x.NomeTerminal) == true).ToList();

            #region contruir lista de tarefas
            foreach (var load in result.Carregamentos)
            {
                listaDeTarefas.Add(new Tarefa(load));
                timeLine.Add(new Evento(load.HorarioCarregamento)); //adiciono um evento vazio, apenas pra dar um tick no relogio
            }

            foreach (var arrival in result.Chegadas)
            {
                //arrival.randLoad = 0.20 + 0.80 * (this.rand.NextDouble()); //blocos do carregamento, são de no mínimo 20%
                //arrival.randUnload = 0.25 + 0.75 * (this.rand.NextDouble());

                int loadTotal = 0;
                int emptyTotal = 0;
                int qtdeAtual = 0;
                int qtdeRestante = 0;

                while (loadTotal < arrival.QtdeVagoesCarregados)
                {
                    qtdeRestante = arrival.QtdeVagoesCarregados - loadTotal;
                    qtdeAtual = Math.Min(qtdeRestante, (int)Math.Floor(arrival.randLoad * arrival.QtdeVagoesCarregados));
                    loadTotal += qtdeAtual;

                    listaDeTarefas.Add(new Tarefa(arrival, qtdeAtual, 0.0));
                }

                while (emptyTotal < arrival.QtdeVagoesVazio)
                {
                    qtdeRestante = arrival.QtdeVagoesVazio - emptyTotal;
                    qtdeAtual = Math.Min(qtdeRestante, (int)Math.Floor(arrival.randUnload * arrival.QtdeVagoesVazio));
                    emptyTotal += qtdeAtual;

                    listaDeTarefas.Add(new Tarefa(arrival, -1 * qtdeAtual, 1.0));
                }

                timeLine.Add(new Evento(arrival.HorarioChegada)); //adiciono um evento vazio, apenas pra dar um tick no relogio

            }

            
            #endregion

            var instantePrimeiraTarefa = listaDeTarefas.Min(x => x.instante);
            
            var vagoesLM = new List<VagaoLM>(); //como vagao nao é uma classe do banco, tenho que criar
            //var vagoesLmLivres = new List<Evento>();
            //var linhasCarregamentoLivres = new List<Evento>();

            for(int i = 0; i < maxMovParalelo; i++)
            {
                //var novoVagao = new VagaoLM (i, instantePrimeiroEvento);
                var novoVagaoLM = new VagaoLM(i);
                vagoesLM.Add(novoVagaoLM);
                //vagoesLmLivres.Add(new Evento(novoVagao, instantePrimeiroEvento));
                this.timeLine.Add(new Evento(novoVagaoLM, instantePrimeiraTarefa)); //LM está inicialmente livre
            }

            foreach (var line in linhasTerminais)
            {
                //line.instanteDeLiberacao = instantePrimeiroEvento; //todas as linhas de carregamento estao livres em t = 0
                //line.prioridade = rand.NextDouble();
                //linhasCarregamentoLivres.Add(new Evento(line, instantePrimeiroEvento));
                this.timeLine.Add(new Evento(line, instantePrimeiraTarefa)); //linha terminal está inicialmente livre 
            }

            foreach(var line in linhasDeManobra)
            {
                line.vagoesVaziosAtual = line.QtdeVagoesVazios;
                line.vagoesCarregadosAtual = line.QtdeVagoesCarregados;
                //line.prioridade = rand.NextDouble();

                if(line.QtdeVagoesCarregados > 0)
                {
                    //cria tarefas de descarga (uma para cada linha de manobra, por enquanto)
                    listaDeTarefas.Add(new Tarefa(new Descarga(line), instantePrimeiraTarefa));
                }
                
            }

            listaDeTarefas = listaDeTarefas.OrderBy(x => x.instante)
                .ThenBy(x => x.prioridade)
                .ToList(); 

            //this.timeLine.Sort((x, y) => x.instante.CompareTo(y.instante));
            //vagoesLM.Sort((x, y) => x.instanteDeLiberacao.CompareTo(y.instanteDeLiberacao));
            //linhasDeCarregamento.Sort((x, y) => x.instanteDeLiberacao.CompareTo(y.instanteDeLiberacao));
            
            #region algoritmo como agrupamento de eventos por instante
            //while (timeLine.Any())
            //{
            //    timeLine.Sort((x, y) => x.instante.CompareTo(y.instante));
            //    var timeLineAgrupada = timeLine.GroupBy(x => x.instante).ToList();
            //    var vagoesLmLivres = new List<Evento>();
            //    var linhasCarregamentoLivres = new List<Evento>();
            //    var filaCarregamentos = new List<Evento>();
            //    var filaChegadas = new List<Evento>();

            //    foreach (var evento in timeLineAgrupada.First()) //para todos os eventos do instante mais recente
            //    {
            //        if (evento.vagaoLM != null)
            //        {
            //            vagoesLmLivres.Add(evento);
            //        }
            //        else if (evento.linha != null)
            //        {
            //            linhasCarregamentoLivres.Add(evento);
            //        }
            //        else if (evento.carregamento != null)
            //        {
            //            filaCarregamentos.Add(evento);
            //        }
            //        else if (evento.chegada != null)
            //        {
            //            filaChegadas.Add(evento);
            //        }

            //        timeLine.Remove(evento);
            //    }

            //} 
            #endregion
            
            #region algoritmo evento por evento

            linhasTerminais.Sort((x, y) => x.prioridade.CompareTo(y.prioridade));
            linhasDeManobra.Sort((x, y) => x.prioridade.CompareTo(y.prioridade));

            var vagoesLmLivres = new List<Evento>(); //TODO: trocar por lista da classe vagao LM (isso quando lista de tarefas e timeline já forem listas de objetos)
            var linhasTerminaisLivres = new List<Evento>(); //TODO: trocar por lista da classe linha (isso quando lista de tarefas e timeline já forem listas de objetos)

            //var filaCarregamentos = new List<Carregamento>();
            //var filaChegadas = new List<Chegada>();

            while (this.timeLine.Any())
            {
                this.timeLine = this.timeLine.OrderBy(x => x.instante)
                    //.ThenBy(x => x.prioridade)
                    .ThenBy(x => x.vagaoLM == null ? 9999 : x.vagaoLM.Idx)
                    .ThenBy(x => x.linhaTerminal == null ? 9999 : x.linhaTerminal.LinhaID)
                    .ToList();

                //Evento evento;

                #region contabilizar o evento mais atual

                var evento = this.timeLine[0];

                #region libera vagao LM
                if (evento.vagaoLM != null)
                {
                    result.rows.Add(new ResultadoOtimizaDataRow(evento.instante, String.Format("Vagão LM #{0} liberado",  evento.vagaoLM.Idx), 0));
                    vagoesLmLivres.Add(evento);
                }
                #endregion
                #region libera linha terminal
                else if (evento.linhaTerminal != null)
                {
                    //if(evento.qtdeVagoesLiberados > 0)
                    //{
                    //    //evento.linhaTerminal.QtdeVagoesCarregados += evento.qtdeVagoesLiberados;
                    //    var mensagem = String.Format("Terminal {0}: fim de carga ({1} vagões)", evento.linhaTerminal.Nome, evento.qtdeVagoesLiberados);
                    //    result.rows.Add(new ResultadoOtimizaDataRow(evento.instante, mensagem , 1));
                    //}
                    //else if(evento.qtdeVagoesLiberados < 0)
                    //{
                    //    //evento.linhaTerminal.QtdeVagoesVazios += -1 * evento.qtdeVagoesLiberados;
                    //    var mensagem = String.Format("Terminal {0}: fim de descarga ({1} vagões)", evento.linhaTerminal.Nome, -1 * evento.qtdeVagoesLiberados);
                    //    result.rows.Add(new ResultadoOtimizaDataRow(evento.instante, mensagem, 1));
                    //}
                    //else
                    //{
                    //    result.rows.Add(new ResultadoOtimizaDataRow(evento.instante, String.Format("Terminal {0} disponível", evento.linhaTerminal.Nome), 0));
                    //    linhasTerminaisLivres.Add(evento);
                    //}

                    result.rows.Add(new ResultadoOtimizaDataRow(evento.instante, String.Format("Terminal {0} disponível", evento.linhaTerminal.Nome), 0));
                    linhasTerminaisLivres.Add(evento);
                    
                }
                #endregion
                //else if (evento.carregamento != null)
                //{
                //    eventosPendentes.Add(evento);
                //}
                //else if (evento.chegada != null)
                //{
                //}
                //else if (evento.partida != null)
                //{
                //    //partidas ainda não são consideradas
                //}

                var ultimoInstanteTratado = evento.instante;
                this.timeLine.RemoveAt(0); //removo o evento que foi tratado
                #endregion

                if(this.timeLine.Any() == false || this.timeLine[0].instante > ultimoInstanteTratado)
                {
                    var tarefasEmOrdem = listaDeTarefas.OrderBy(x => x.instante)
                        .ThenBy(x => x.prioridade)
                        //TODO: add terceira prioridade?
                        .ToList();

                    #region resolver as tarefas da fila
                    foreach (var job in tarefasEmOrdem)
                    {
                        //TODO: aqui acredito que tenha que existir uma verificacao se a tarefa esta disponivel no instante considerado (tarefas como carregamento, estão disponíveis antes do instante, mas a maioria nao) - pq o instante do carregamento é um limite, data de entrega, os demais são instantes de acontecimento mesmo
                        if (job.carregamento != null)
                        {
                            if (linhasTerminaisLivres.Any(x => x.linhaTerminal.LinhaID == job.carregamento.LinhaID) == false)
                            {
                                continue; //se o terminal está ocupado
                            }

                            if (vagoesLmLivres.Any() == false)
                                continue;

                            var vagaoDesignado = vagoesLmLivres[0];

                            var qtdeAtualVagoesVaziosNoPatio = linhasDeManobra.Sum(x => x.vagoesVaziosAtual);

                            if (qtdeAtualVagoesVaziosNoPatio < job.carregamento.QtdeVagoes)
                            {
                                continue; //não trato o eventoAtual - não deve acontecer
                            }

                            this.FazCarregamento(vagaoDesignado, job, linhasDeManobra, ultimoInstanteTratado, result);

                            //carregamento foi encaminhado, então linha e vagao não estão mais livres
                            linhasTerminaisLivres.RemoveAll(x => x.linhaTerminal.LinhaID == job.carregamento.LinhaID); //apesar de usar All, só deve apagar um
                            vagoesLmLivres.RemoveAt(0);
                        }
                        else if (job.descarga != null && ultimoInstanteTratado >= job.instante)
                        {
                            if (linhasTerminaisLivres.Any(x => x.linhaTerminal.Capacidade >= job.descarga.linhaManobra.QtdeVagoesCarregados) == false)
                            {
                                continue; //se o terminal está ocupado
                            }

                            if (vagoesLmLivres.Any() == false)
                                continue;

                            var vagaoLivre = vagoesLmLivres[0];

                            this.FazDescarga(vagaoLivre, job, linhasTerminaisLivres, ultimoInstanteTratado, result);

                            //carregamento foi encaminhado, então linha e vagao não estão mais livres
                            vagoesLmLivres.RemoveAt(0);

                        }
                        else if (job.movimento != null && ultimoInstanteTratado >= job.instante)
                        {
                            if (vagoesLmLivres.Any() == false)
                                continue;

                            var vagaoDesignado = vagoesLmLivres[0];

                            vagoesLmLivres.RemoveAt(0); //movimento foi encaminhado, então ocupo o vagao

                            vagaoDesignado.instante = ultimoInstanteTratado.AddMinutes(this.tempoMovEntreLinhas);

                            this.timeLine.Add(vagaoDesignado);

                            var acao = "";

                            if (job.movimento.qtdeVagoes > 0)
                            {
                                acao = String.Format("Levar {0} vagoes carregados do terminal {1} para a linha {2} utilizando vagão LM #{3}",
                                    job.movimento.qtdeVagoes, job.movimento.linhaOrigem.Nome, job.movimento.linhaDestino.Nome, vagaoDesignado.vagaoLM.Idx);
                            }
                            else
                            {
                                acao = String.Format("Levar {0} vagoes vazios do terminal {1} para a linha {2} utilizando vagão LM #{3}",
                                    -1 * job.movimento.qtdeVagoes, job.movimento.linhaOrigem.Nome, job.movimento.linhaDestino.Nome, vagaoDesignado.vagaoLM.Idx);
                            }

                            result.rows.Add(new ResultadoOtimizaDataRow(ultimoInstanteTratado, acao, 1));

                            job.concluida = 1;
                        }
                        else if (job.chegada != null && ultimoInstanteTratado >= job.chegada.HorarioChegada)
                        {
                            //TODO: tratar a chegada - uma chegada implica em: 
                            //1 decidir em quais linhas de manobra alocar os vagoes
                            //2 mais vagoes carregados que precisam ser descarregados
                            //3 contabilizar a qtde vagoes vazios

                            var qtdeVagoesAbs = Math.Abs(job.QtdeVagoesConsiderada);

                            var linhaDeManobraDesignada = linhasDeManobra.FirstOrDefault(x => 
                                x.QtdeVagoesCarregados + x.QtdeVagoesVazios + qtdeVagoesAbs <= x.Capacidade);

                            if (linhaDeManobraDesignada != null)
                            {
                                if (job.QtdeVagoesConsiderada > 0)
                                {
                                    result.rows.Add(new ResultadoOtimizaDataRow(ultimoInstanteTratado, 
                                        String.Format("Chegada {0}. Levar {1} vagões carregados para a linha {2}", 
                                        job.chegada.prefixo,
                                        qtdeVagoesAbs, 
                                        linhaDeManobraDesignada.Nome), 1));

                                    linhaDeManobraDesignada.QtdeVagoesCarregados += qtdeVagoesAbs;
                                    //job.chegada.QtdeVagoesCarregados = 0;

                                    listaDeTarefas.Add(new Tarefa(new Descarga(linhaDeManobraDesignada), ultimoInstanteTratado));
                                    //timeLine.Add(new Evento(ultimoInstanteTratado));
                                }

                                if (job.QtdeVagoesConsiderada < 0)
                                {
                                    result.rows.Add(new ResultadoOtimizaDataRow(ultimoInstanteTratado, 
                                        String.Format("Chegada {0}. Levar {1} vagões vazios para a linha {2}", 
                                        job.chegada.prefixo,
                                        qtdeVagoesAbs, 
                                        linhaDeManobraDesignada.Nome), 1));

                                    linhaDeManobraDesignada.QtdeVagoesVazios += qtdeVagoesAbs;
                                    //job.chegada.QtdeVagoesVazio = 0;
                                }

                                job.concluida = 1;
                            }
                            
                        }
                        else if (job.partida != null)
                        {
                            job.concluida = 1;
                        }
                    }

                    listaDeTarefas.RemoveAll(x => x.concluida == 1);

                    #endregion
                } //fim de if
            } //fim de while 
            #endregion

            //result.rows.Sort((x, y) => x.horario.CompareTo(y.horario));
            
            result.FO = result.rows.Sum(x => x.qtdeManobras);
            result.rows.Add(new ResultadoOtimizaDataRow(DateTime.Now, "Total de manobras", result.FO));

            return result;
        }

        private void FazCarregamento(Evento eventoVagao, Tarefa job, List<Linha> linhasDeManobra, DateTime ultimoInstante, ResultadoOtimizaData result)
        {
            var linhasDeManobraComVagoesVazios = linhasDeManobra.Where(x => x.vagoesVaziosAtual > 0).ToList();

            int linhasUsadas = 0;
            int qtdeVagoesAtribuidasAcumulada = 0;

            foreach (var line in linhasDeManobraComVagoesVazios)
            {
                linhasUsadas++;
                string acao = "";

                var qtdeRestante = job.carregamento.QtdeVagoes - qtdeVagoesAtribuidasAcumulada;
                var qtdeDaLinha = Math.Min(qtdeRestante, line.vagoesVaziosAtual); //minimo entre quanto falta e quanto tem disponivel na linha
                qtdeVagoesAtribuidasAcumulada += qtdeDaLinha;

                var tempoCarregamento = (double)qtdeDaLinha / cargaDescarga;
                var instanteTerminoCarregamento = ultimoInstante.AddMinutes(tempoMovEntreLinhas + (60 * tempoCarregamento));
                //this.timeLine.Add(new Evento(line, job.carregamento.Linha, instanteTerminoCarregamento, qtdeDaLinha)); //termino do carregamento dos n vagoes

                var novaTarefaMov = new Movimento(job.carregamento.Linha, line, qtdeDaLinha); //cria uma tarefa de solicitacao de movimento da linha terminal para a linha de manobra
                listaDeTarefas.Add(new Tarefa(novaTarefaMov, instanteTerminoCarregamento, job.prioridade));

                acao = String.Format("Levar {0} vagoes da linha {1} para carregamento no terminal {2} utilizando vagão LM #{3}",
                    qtdeDaLinha, line.Nome, job.carregamento.Linha.Nome, eventoVagao.vagaoLM.Idx);

                line.vagoesVaziosAtual -= qtdeDaLinha;
                result.rows.Add(new ResultadoOtimizaDataRow(ultimoInstante, acao, 1));
                if (qtdeVagoesAtribuidasAcumulada == job.carregamento.QtdeVagoes)
                    break;
            }

            double tempoCarregamentoTotal = (double)job.carregamento.QtdeVagoes/cargaDescarga; //os tempos de movimentacoes dos vagoes ainda não sao considerados
            this.timeLine.Add(new Evento(job.carregamento.Linha, ultimoInstante.AddHours(tempoCarregamentoTotal))); //evento de liberacao da linha terminal

            eventoVagao.instante = ultimoInstante.AddMinutes(linhasUsadas * tempoMovEntreLinhas);
            this.timeLine.Add(new Evento(eventoVagao.vagaoLM, eventoVagao.instante)); //evento de liberacao da LM

            job.concluida = 1;
        }


        private void FazDescarga(Evento eventoVagao, Tarefa job, List<Evento> linhasTerminaisLivres, DateTime ultimoInstante, ResultadoOtimizaData result)
        {
            //int linhasUsadas = 0;
            //int qtdeVagoesAtribuidasAcumulada = 0;
            int t = 0;

            for (t = 0; t < linhasTerminaisLivres.Count; t++)
            {
                var terminal = linhasTerminaisLivres[t];
                if (terminal.linhaTerminal.Capacidade < job.descarga.linhaManobra.QtdeVagoesCarregados)
                    continue;
            
                var tempoDescarga = (double)job.descarga.linhaManobra.QtdeVagoesCarregados / this.cargaDescarga;
                var instanteTerminoDescarga = ultimoInstante.AddMinutes(tempoMovEntreLinhas + (60 * tempoDescarga));
                //var instanteLiberacaoDoTerminal = instanteTerminoDescarga.AddMinutes(tempoMovEntreLinhas);
                //this.timeLine.Add(new Evento(job.descarga.linhaOrigem, terminal.linhaTerminal, instanteTerminoDescarga, -1 * job.descarga.linhaOrigem.QtdeVagoesCarregados)); //termino da descarga dos n vagoes
                //this.timeLine.Add(new Evento(terminal.linhaTerminal, instanteLiberacaoDoTerminal)); //evento de liberacao da linha terminal

                var novaTarefaMov = new Movimento(terminal.linhaTerminal, job.descarga.linhaManobra, - 1 * job.descarga.linhaManobra.QtdeVagoesCarregados); //cria uma tarefa de solicitacao de movimento da linha terminal para a linha de manobra
                listaDeTarefas.Add(new Tarefa(novaTarefaMov, instanteTerminoDescarga , job.prioridade));

                string acao = String.Format(
                    "Levar {0} vagoes da linha {1} para descarga no terminal {2} utilizando vagão LM #{3}",
                    job.descarga.linhaManobra.QtdeVagoesCarregados, 
                    job.descarga.linhaManobra.Nome, 
                    terminal.linhaTerminal.Nome, 
                    eventoVagao.vagaoLM.Idx
                    );

                result.rows.Add(new ResultadoOtimizaDataRow(ultimoInstante, acao, 1));

                break;
            }

            linhasTerminaisLivres.RemoveAt(t);

            eventoVagao.instante = ultimoInstante.AddMinutes(tempoMovEntreLinhas);
            this.timeLine.Add(new Evento(eventoVagao.vagaoLM, eventoVagao.instante)); //evento de liberacao da LM

            job.concluida = 1;
        }

    }

    
}