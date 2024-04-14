from SALib.sample import saltelli
from SALib.analyze import sobol
from SALib.test_functions import Ishigami
import numpy as np
import math


problem = {
    'num_vars': 4,
    'names': ['x1', 'x2', 'x3','x4'],
    'bounds': [[230545.18, 328743.38],
               [155041.634, 268467.5821],
               [2497831,6062262],
               [1352.26,6090.86]
               ]
}


def evaluate(X):

    return np.array([0.0022223*x[0]-0.0004257*x[1] + 1.40*(math.pow(10, -25))*(math.pow(x[2], 4)) + 0.0019446*x[3]+3170.996 for x in X])



param_values = saltelli.sample(problem, 1000)

Y = evaluate(param_values)
print(param_values.shape, Y.shape)
Si = sobol.analyze(problem, Y, print_to_console=True)
print()


print('S1:', Si['S1'])


print("x1-x2:", Si['S2'][0, 1])
print("x1-x3:", Si['S2'][0, 2])
print("x1-x4:", Si['S2'][0, 3])
print("x2-x3:", Si['S2'][1, 2])
print("x2-x3:", Si['S2'][1, 3])
print("x3-x4:", Si['S2'][2, 3])


from SALib.plotting.bar import plot as barplot
import matplotlib.pyplot as plot

Si_df = Si.to_df()
barplot(Si_df[0])
plot.show()
