# Avatares Reelix

Nove desenhos vetoriais originais para o Reelix, sem reproduzir personagens de franquias existentes.

| Chave | Personagem | Tema |
| --- | --- | --- |
| `noir` | Detetive Neblina | Mistério |
| `cosmic` | Órbita | Ficção científica |
| `phantom` | Espectro | Terror |
| `comedy` | Riso em Cena | Comédia |
| `animation` | Pipoca | Animação |
| `adventure` | Exploradora Lumi | Aventura |
| `robot` | Robô Pixel | Ficção científica |
| `vampire` | Vampiro Noturno | Terror |
| `maestro` | Maestro Jazz | Musical |

A seleção usa uma faixa horizontal rolável: desenho, nome, tema e indicação de seleção.
O componente compartilhado `UserAvatar` renderiza os SVGs com `viewBox="0 0 100 100"`.
A opção `initials` restaura as iniciais do nome e remove a chave persistida.
Novas opções exigem atualização coordenada do catálogo mobile, domínio e restrição SQL.
A API não aceita URLs externas como chave de avatar.
