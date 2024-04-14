# 模型灵敏度分析
from SALib.sample import saltelli
from SALib.analyze import sobol
from SALib.test_functions import Ishigami
import numpy as np
import math

# 定义模型输入
"""
定义模型输入。Ishigami功能具有四个输入
在SALib中，我们定义了一个dict定义输入的数量，输入的名称以及每个输入的边界，
如下所示
"""
problem = {
    'num_vars': 4,
    'names': ['x1', 'x2', 'x3','x4'],
    'bounds': [[524520, 580835],
               [330459.9, 482208.47],
               [695060,1754666],
               [340.06,899.05]
               ]
}


def evaluate(X):
    """
        要进行灵敏度分析的模型，接受一个数组，每个数组元素作为模型的一个输入,
        将输入的x映射为一个y,通过数组返回

    """
    return np.array([2.45*(math.pow(10, -27))*(math.pow(x[0], 5))-3.75*(math.pow(10, -32))*(math.pow(x[1], 6)) + \
        0.0019694*x[1]-4.45 * \
        (math.pow(10, -12))*(math.pow(x[2], 2))-1.47 * \
        (math.pow(10, -19))*(math.pow(x[3], 7))+1360.053 for x in X])


# 样本生成
"""
    param_values是一个NumPy矩阵。如果运行 param_values.shape，
    我们将看到矩阵乘以3等于8000。Saltelli采样器生成了8000个样本。
    Saltelli采样器生成 N∗(2D+2)样本，在此示例中，N为1000（我们提供的参数），
    D为3（模型输入的数量）
"""
param_values = saltelli.sample(problem, 1000)

# 运行模型
Y = evaluate(param_values)
print(param_values.shape, Y.shape)
Si = sobol.analyze(problem, Y, print_to_console=True)
print()

# 一阶灵敏度
print('S1:', Si['S1'])

# 二阶灵敏度
print("x1-x2:", Si['S2'][0, 1])
print("x1-x3:", Si['S2'][0, 2])
print("x1-x4:", Si['S2'][0, 3])
print("x2-x3:", Si['S2'][1, 2])
print("x2-x3:", Si['S2'][1, 3])
print("x3-x4:", Si['S2'][2, 3])

"""
    置信区间: 对这个样本的某个总体参数的区间估计，置信区间展现的是这个参数的真实值有一定概率落在测量结果的周围的程度。
             置信区间给出的是被测量参数的测量值的可信程度。一般常取为95%或者90%或者99%.是预先取定的值.
             如:如果在一次大选中某人的支持率为55%，而置信水平0.95上的置信区间是（50%,60%），那么他的真实支持率有百分之九十五的机率落在百分之五十和百分之六十之间
    显著性水平: 一个预先取定的值（一般取0.05/0.01）,一般用alpha表示.跟置信概率恰好方向相反（加起来是1）,在假设检验中表示在零假设成立下拒绝它所犯的一类错误的上界.在用p值检验时,
              如果p值比显著性水平小,就可以放心拒绝原假设.反之,不拒绝.
    置信度: 置信区间上下限的差值。
"""
from SALib.plotting.bar import plot as barplot
import matplotlib.pyplot as plot

Si_df = Si.to_df()
barplot(Si_df[0])
plot.show()
