﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RumoPatios.Models
{
    /// <summary>
    /// Uma tarefa é uma ação (são tarefas: movimentos de vagoes). As tarefas são originadas pelos eventos (carregamento, uma chegada, uma partida, etc)
    /// </summary>
    public class Tarefa
    {
        #region construtores
        public Tarefa() { }

        public Tarefa(Carregamento load)
        {
            this.instante = load.HorarioCarregamento;
            this.carregamento = load;
            this.prioridade = load.prioridade;
        }

        public Tarefa(Chegada arrival, int nVagoes, double priority)
        {
            this.instante = arrival.HorarioChegada;
            this.chegada = arrival;
            this.prioridade = priority;
            this.QtdeVagoesConsiderada = nVagoes;
        }

        /// <summary>
        /// Tarefas de descarga são criadas a partir dos vagoes carregados que estão inicialmente no patio e assim que novos vagoes carregados chegam atraves das Chegadas
        /// </summary>
        /// <param name="descarregamento"></param>
        /// <param name="inst"></param>
        public Tarefa(Descarga descarregamento, DateTime inst)
        {
            this.instante = inst;
            this.descarga = descarregamento;
            this.prioridade = descarregamento.linhaManobra.prioridade; //uma descarga tem a mesma prioridade da linha de manobra que a originou
        }

        public Tarefa(Movimento mov, DateTime inst, double priority)
        {
            this.instante = inst;
            this.movimento = mov;
            this.prioridade = priority; //um movimento tem a prioridade da tarefa que o originou
        }
        #endregion


        #region propriedades
        /// <summary>
        /// Os carregamentos devem ser concluidos antes do instante, as chegadas devem ser tratadas a partir do instante. 
        /// </summary>
        internal DateTime instante { get; set; }

        /// <summary>
        /// prioridade da tarefa
        /// </summary>
        public double prioridade { get; set; }

        /// <summary>
        /// 1 se foi, 0 caso contrario
        /// </summary>
        public int concluida { get; set; }

        #region tarefa esta associada a alguma das tarefas do banco
        public Chegada chegada { get; set; }
        /// <summary>
        /// utilizada pelas chegadas, já que elas são fracionadas em varios blocos
        /// </summary>
        public int QtdeVagoesConsiderada { get; set; }
        
        public Carregamento carregamento { get; set; }
        public Partida partida { get; set; }
        public Descarga descarga { get; set; }
        public Movimento movimento { get; set; }
        #endregion  
        #endregion
    }
}