import statsmodels.api as sm
import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv('东北林区.csv')

data=df[0:14]
data=data.iloc[:,0:2]
print(data)

# Aggregating the dataset at daily level
df['Timestamp'] = pd.to_datetime(df['date'], format='%Y')  # 4位年用Y，2位年用y
df.index = df['date']
df = df.resample('D').mean()  # 按天采样，计算均值

data['Timestamp'] = pd.to_datetime(data['date'], format='%Y')
data.index = data['Timestamp']
train = data.resample('D').mean()  #


# Plotting data
train.Count.plot(figsize=(15, 8), title='carbon', fontsize=14)

plt.show()