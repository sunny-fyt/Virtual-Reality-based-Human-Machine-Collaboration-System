import pandas as pd
import matplotlib.pyplot as plt

# Subsetting the dataset
# Index 11856 marks the end of year 2013
df = pd.read_csv('东北林区.csv')

data=df[0:15]
data=data.iloc[:,0:1]
data.plot( title= 'Carbon sequestration in Northeast Forest Region', label="org_data",fontsize=14)
plt.show()