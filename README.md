![quadtree](https://user-images.githubusercontent.com/37039414/192510011-c237b28b-1251-407c-b2db-bb67d68c649a.gif)


[![Unity Version](https://img.shields.io/badge/unity-2020.3.17f1+-blue)](https://unity3d.com/get-unity/download)
[![GitHub](https://img.shields.io/badge/license-MIT-green)](https://github.com/sxm-sxpxxl/parallax-effect/blob/master/LICENSE.md)

## О проекте

Коллекция известных структур данных пространственного разбиения в управляемой и нативной формах.

Разбиение пространства производится на непересекающиеся области, организованные в иерархичные структуры данных - деревья пространственного разбиения. 
Предполагает использование в решении задачи определения пересечения объектов с заданной областью пространства. 
В общем случае эффективность поиска достигается за счет рассмотрения только интересующих областей пространства.

## Структуры
Коллекция охватывает следующие разновидности структур деревьев пространственного разбиения в 2D и 3D формах:
- **Quadtree/Octree** - простое квадродерево/октодерево (подробнее по [ссылке](https://en.wikipedia.org/wiki/Quadtree))
- **Compressed Quadtree/Octree** - сжатое квадродерево/октодерево, избегающее создания пустых листьев при добавлении близко расположенных объектов
- **Skip Quadtree/Octree** - сжатое квадродерево/октодерево с пропусками, имеющее несколько уровней детализации 
согласно более известному алгоритму [списка с пропусками](https://en.wikipedia.org/wiki/Skip_list) (подробнее по [ссылке](https://www.ics.uci.edu/~goodrich/pubs/skip-journal.pdf))

## Установка
### Установка через UPM (используя Git URL)
Пожалуйста, добавьте следующую строку в манифест файл (`Packages/manifest.json`) в раздел `dependencies`:

```"com.sxm.spatial-partition-structures": "https://github.com/sxm-sxpxxl/spatial-partition-structures.git"```

или просто скачайте и разархивируйте репозиторий в папку `Packages` проекта.
