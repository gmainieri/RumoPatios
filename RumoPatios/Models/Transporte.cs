﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RumoPatios.Models
{
    /// <summary>
    /// Solicitação de transporte de vagoes entre linhas (terminal=>manobra ou manobra=>terminal). 
    /// Um transporte pode ser classificado como uma tarefa/ordem/ação. 
    /// Alguns eventos podem gerar transportes (carregamento, chegada, partida, etc).
    /// </summary>
    public class Transporte
    {
        public Transporte() { }

        /// <summary>
        /// Transporte de vagoes entre linhas (terminal->manobra ou manobra->terminal)
        /// </summary>
        /// <param name="linhaFrom">linha origem</param>
        /// <param name="linhaTo">linha destino</param>
        /// <param name="qtde">quantidade de vagoes, se carregados, positiva, negativa caso contrário</param>
        public Transporte(Linha linhaFrom, Linha linhaTo, int qtde, bool vazios, DateTime instante, double priority)
        {
            //evita a criacao de um transporte com as duas linhas definidas (por enquanto não existe nenuma situação na qual origem e destino são conhecidos)
            if (linhaFrom != null && linhaTo != null)
                return; 
            
            this.linhaOrigem = linhaFrom;
            this.linhaDestino = linhaTo;
            this.qtdeVagoes = qtde;
            this.Vazios = vazios;
            this.instante = instante;
            this.prioridade = priority;
        }

        /// <summary>
        /// linha de origem = linha que contém os vagoes
        /// </summary>
        public Linha linhaOrigem { get; set; }
        
        /// <summary>
        /// apenas no caso dos carregamentos, quem solicita o transporte é a linha de destino
        /// </summary>
        public Linha linhaDestino { get; set; }

        /// <summary>
        /// positiva: carregados, negativa: vazios
        /// </summary>
        public int qtdeVagoes { get; set; }

        /// <summary>
        /// 1 se a qtde de vagoes refere-se a vagoes vazios, zero c.c.
        /// </summary>
        public bool Vazios { get; set; }

        /// <summary>
        /// instante programado a partir do qual o transporte deve ser realizado
        /// </summary>
        public DateTime instante { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double prioridade { get; set; }

        public int concluido { get; set; }


    }
}